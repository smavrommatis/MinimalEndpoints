using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMinimalEndpoints();

var app = builder.Build();

app.UseMinimalEndpoints();

app.Run();

/// <summary>
/// Exposed so the end-to-end test project can boot this app via
/// <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public partial class Program;
