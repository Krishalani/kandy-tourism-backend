using KandyTourism.Api.Extensions;
using KandyTourism.Api.Infrastructure.Seed;

if (args.Contains("generate-seed-sql"))
{
    var path = Path.Combine(Directory.GetCurrentDirectory(), SeedSqlScriptGenerator.RelativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await File.WriteAllTextAsync(path, SeedSqlScriptGenerator.Generate());
    Console.WriteLine($"Wrote {path}");
    return;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices()
    .AddInfrastructure(builder.Configuration)
    .AddApiSecurity(builder.Configuration)
    .AddApiPresentation(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Kandy Day Trip Planner API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.InitializeDatabaseAsync();
await app.RunAsync();

public partial class Program;
