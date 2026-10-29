var builder = WebApplication.CreateBuilder(args);

// [ApiController] answers 404 and 400 with a ProblemDetails body since ASP.NET Core 2.2; on 2.1 they
// were empty, and the error responses below stay the shape the app has always returned.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);

var app = builder.Build();

app.MapControllers();
app.MapGet("/healthz", () => "ok");
app.MapGet("/buildz", () => Results.Json(new { build = Environment.GetEnvironmentVariable("BUILD_SHA") ?? "unknown" }));

app.Run();
