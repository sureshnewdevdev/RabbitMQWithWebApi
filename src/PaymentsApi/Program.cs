using PaymentsApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.AddSingleton<PaymentStore>();
builder.Services.AddHostedService<CheckoutConsumerService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    service = "Payments API",
    description = "Consumes cart.checkout events and stores approved payments in memory."
}));

app.MapGet("/payments", (PaymentStore store) => Results.Ok(store.GetAll()))
    .WithName("GetPayments")
    .WithOpenApi();

app.Run();
