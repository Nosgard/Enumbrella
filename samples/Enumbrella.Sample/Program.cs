using System.Text.Json;
using System.Text.Json.Serialization;
using Enumbrella;
using Enumbrella.Sample;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi(options => options.AddEnumDescriptions());
builder.Services.ConfigureHttpJsonOptions(json => json.SerializerOptions.Converters.Add(
    new JsonStringEnumConverter<PaymentMethod>(JsonNamingPolicy.CamelCase)));

var app = builder.Build();
app.MapOpenApi();
app.MapGet("/orders/{id}", (int id) => new Order(id, OrderStatus.Pending));
app.MapGet("/payments/{id}", (int id) => new Payment(id, PaymentMethod.Card));

app.Run();
