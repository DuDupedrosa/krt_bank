using krt_cartoes_api.Application.Services;
using krt_cartoes_api.Core.Interfaces;
using krt_cartoes_api.Infrastructure.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ICartoesService, CartoesService>();
builder.Services.AddHostedService<CartoesAccountCreatedConsumerService>();
builder.Services.AddHostedService<CartoesAccountUpdatedConsumerService>();
builder.Services.AddHostedService<CartoesAccountDeletedConsumerService>();

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
