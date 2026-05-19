using Pr3.ConfigAndSecurity.Config;
using Pr3.ConfigAndSecurity.Domain;
using Pr3.ConfigAndSecurity.Middlewares;
using Pr3.ConfigAndSecurity.Services;

var builder = WebApplication.CreateBuilder(args);

ConfigurationPriority.ApplyPriority(builder, args);

var appSettings = builder.Configuration.GetSection(AppSettings.SectionName).Get<AppSettings>()
    ?? new AppSettings();

bool detailedErrors = appSettings.Mode == AppMode.Study;
try
{
    AppSettingsValidator.ValidateAndThrow(appSettings, detailedErrors);
}
catch (InvalidOperationException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(ex.Message);
    Console.ResetColor();
    Environment.Exit(1);
    return;
}

builder.Services.AddSingleton(appSettings);
builder.Services.AddSingleton(new ModeContext(appSettings));

builder.Services.AddCors(options =>
{
    options.AddPolicy("TrustedOrigins", policy =>
    {
        policy.WithOrigins(appSettings.GetTrustedOriginsArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.WebHost.UseUrls($"http://0.0.0.0:{appSettings.Port}");

var app = builder.Build();

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<OriginValidationMiddleware>();
app.UseCors("TrustedOrigins");
app.UseMiddleware<RateLimitingMiddleware>(
    appSettings.ReadLimitPerMinute,
    appSettings.CreateLimitPerMinute);

var modeContext = app.Services.GetRequiredService<ModeContext>();

var items = new List<Item>
{
    new(1, "First item"),
    new(2, "Second item", "Description here")
};
int nextId = 3;

app.MapGet("/items", () =>
{
    return Results.Ok(items);
});

app.MapGet("/items/{id:int}", (int id) =>
{
    var item = items.FirstOrDefault(i => i.Id == id);
    if (item is null)
    {
        var msg = modeContext.IsStudy ? $"Item with id {id} not found." : "Not found.";
        return Results.NotFound(new { error = msg });
    }
    return Results.Ok(item);
});

app.MapPost("/items", (CreateItemRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        var msg = modeContext.IsStudy ? "Name is required and cannot be empty." : "Invalid input.";
        return Results.BadRequest(new { error = msg });
    }

    var item = new Item(nextId++, request.Name, request.Description);
    items.Add(item);
    return Results.Created($"/items/{item.Id}", item);
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    mode = modeContext.Mode.ToString()
}));

app.MapGet("/mode", () => Results.Ok(new
{
    mode = modeContext.Mode.ToString(),
    description = modeContext.IsStudy
        ? "Study mode: detailed errors and soft limits enabled."
        : "Production mode: minimal error messages, strict limits."
}));

app.Run();

public record CreateItemRequest(string Name, string? Description = null);

