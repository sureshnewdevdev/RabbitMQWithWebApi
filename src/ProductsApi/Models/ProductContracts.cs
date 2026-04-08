namespace ProductsApi.Models;

public sealed record CreateProductRequest(string Name, decimal Price);

public sealed record Product(Guid Id, string Name, decimal Price, DateTimeOffset CreatedAtUtc);

public sealed record ProductCreatedEvent(Guid ProductId, string Name, decimal Price, DateTimeOffset CreatedAtUtc);
