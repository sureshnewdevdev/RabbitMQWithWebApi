using CartsApi.Models;
using CartsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<ProductCatalogStore>();
builder.Services.AddSingleton<CartStore>();
builder.Services.AddSingleton<CheckoutPublisher>();
builder.Services.AddHostedService<ProductCreatedConsumerService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    service = "Carts API",
    description = "Maintains in-memory carts, consumes product.created and publishes cart.checkout."
}));

app.MapGet("/catalog", (ProductCatalogStore catalog) => Results.Ok(catalog.GetAll()));

app.MapPost("/carts/{cartId:guid}/items", (Guid cartId, AddCartItemRequest request, ProductCatalogStore catalog, CartStore carts) =>
{
    if (request.Quantity <= 0)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["quantity"] = ["Quantity must be greater than zero."]
        });
    }

    if (!catalog.TryGet(request.ProductId, out var product) || product is null)
    {
        return Results.NotFound(new { message = "Product not found in cart catalog yet. Create product first." });
    }

    var cart = carts.AddItem(cartId, product, request.Quantity);
    return Results.Ok(cart);
})
.WithName("AddItemToCart")
.WithOpenApi();

app.MapGet("/carts/{cartId:guid}", (Guid cartId, CartStore carts) =>
{
    var cart = carts.Get(cartId);
    return cart is null ? Results.NotFound() : Results.Ok(cart);
});

app.MapPost("/carts/{cartId:guid}/checkout", (Guid cartId, CheckoutCartRequest request, CartStore carts, CheckoutPublisher publisher) =>
{
    if (string.IsNullOrWhiteSpace(request.CardHolder) || string.IsNullOrWhiteSpace(request.CardNumber))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["payment"] = ["CardHolder and CardNumber are required."]
        });
    }

    var cart = carts.Get(cartId);
    if (cart is null || cart.Items.Count == 0)
    {
        return Results.NotFound(new { message = "Cart not found or empty." });
    }

    var checkoutEvent = new CheckoutEvent(
        Guid.NewGuid(),
        cartId,
        cart.TotalAmount,
        string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(),
        request.CardHolder.Trim(),
        DateTimeOffset.UtcNow);

    publisher.Publish(checkoutEvent);
    carts.Clear(cartId);

    return Results.Accepted($"/carts/{cartId}", checkoutEvent);
})
.WithName("CheckoutCart")
.WithOpenApi();

app.Run();
