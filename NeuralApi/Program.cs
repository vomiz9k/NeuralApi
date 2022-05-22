using NeuralApi.RabbitMq;
using RabbitMQ.Client;
using Microsoft.EntityFrameworkCore;
using NeuralApi.Database;


Thread.Sleep(20000);


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddScoped<IRabbitMqService, RabbitMqService>();
builder.Services.AddDbContext<ReadyContext>(opt => opt.UseInMemoryDatabase("ReadyFiles"));
builder.Services.AddDbContext<PendingContext>(opt => opt.UseInMemoryDatabase("PendingFiles"));
builder.Services.AddHostedService<RabbitMqConsumerWorker>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

while(true)
{
    Thread.Sleep(1000);
}