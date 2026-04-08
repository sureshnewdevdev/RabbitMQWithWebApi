using ProductsApi.Models;
using ProductsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<ProductStore>();
builder.Services.AddSingleton<ProductEventPublisher>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    service = "Products API",
    description = "Stores products in-memory and publishes product.created events to RabbitMQ."
}));

app.MapGet("/products", (ProductStore store) => Results.Ok(store.GetAll()));

app.MapPost("/products", (CreateProductRequest request, ProductStore store, ProductEventPublisher publisher) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Price <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["product"] = ["Name is required and Price must be greater than zero."]
        });
    }

    var product = store.Add(request.Name, request.Price);
    publisher.PublishProductCreated(product);

    return Results.Created($"/products/{product.Id}", product);
})
.WithName("CreateProduct")
.WithOpenApi();

app.Run();
