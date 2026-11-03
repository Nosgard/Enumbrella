using Enumbrella.Sample;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapGet("/orders/{id}", (int id) => new Order(id, OrderStatus.Pending));

app.Run();
