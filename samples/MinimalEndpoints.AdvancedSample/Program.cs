using MinimalEndpoints.AdvancedSample.Services;
using MinimalEndpoints.Generated;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MinimalEndpoints
builder.Services.AddMinimalEndpoints();

// Register application services
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddScoped<IAuthenticationService, SimpleAuthenticationService>();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMinimalEndpoints();

app.Run();

