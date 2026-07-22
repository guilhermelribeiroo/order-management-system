using Application.Commands;
using Application.Common.Behaviors;
using FluentValidation;
using Infrastructure.DBContext;
using Infrastructure.Interfaces;
using Infrastructure.Messaging;
using Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using WebAPI.BackgroundServices;
using WebAPI.Filters;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<OrderServiceDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("OrderDatabase")));

builder.Services.AddScoped<IUnitOfWork>(
    sp => sp.GetRequiredService<OrderServiceDbContext>()
);

// EventBus
builder.Services.AddSingleton<IEventBus>(sp =>
{
    var settings = builder.Configuration.GetSection("RabbitMQ").Get<RabbitMqSettings>();
    return new RabbitMqEventBus(settings);
});

// Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommandHandler).Assembly);
});

builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderCommandValidator).Assembly);

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>)
);

// Background Service
builder.Services.AddHostedService<
    OutboxProcessorBackgroundService>();

builder.Services.AddControllers(options => options.Filters.Add(typeof(JsonExceptionFilter)));
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply Migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderServiceDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
