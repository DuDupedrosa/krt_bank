using krt_api.Application.Services;
using krt_api.Core.Accounts.Interfaces;
using krt_api.Core.Interfaces;
using krt_api.Infrastructure;
using krt_api.Infrastructure.Cache;
using krt_api.Infrastructure.Mappings;
using krt_api.Infrastructure.Messaging;
using krt_api.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
});

builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AccountsProfile>();
});

builder.Services.AddScoped<IAccountsService, AccountsService>();
builder.Services.AddScoped<IAccountCacheService, AccountCacheService>();

builder.Services.AddScoped<IAccountsRepository, AccountsRepository>();

builder.Services.AddSingleton<IAccountProducer, AccountProducer>();

var app = builder.Build();

// rodar as migrations automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); 
}

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
