using MinimalEndpoints.AdvancedSample.Services;
using MinimalEndpoints.Generated;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Built-in OpenAPI document. The per-endpoint .AddOpenApiOperationTransformer(...) calls run on
// this pipeline, so the operation summaries/descriptions actually appear in the served document.
builder.Services.AddOpenApi();

// Output caching middleware backing the .CacheOutput(...) calls on the group and ListProducts.
builder.Services.AddOutputCache();

// Register MinimalEndpoints
builder.Services.AddMinimalEndpoints();

// Register application services
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();

var app = builder.Build();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Must run before the endpoints execute so cached responses are served.
app.UseOutputCache();

app.UseMinimalEndpoints();

app.Run();
