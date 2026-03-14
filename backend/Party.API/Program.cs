using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Party.API.Application.Interfaces;
using Party.API.Application.Services;
using Party.API.Application.Validators;
using Party.API.Infrastructure.Messaging;
using Party.API.Infrastructure.Persistence;
using Party.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<PartyDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IPartyService, PartyService>();

// RabbitMQ (singleton — connection reuse)
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// Validation
builder.Services.AddValidatorsFromAssemblyContaining<CreatePartyValidator>();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
	c.SwaggerDoc("v1", new() { Title = "Party.API", Version = "v1" });
});

var app = builder.Build();

// Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Auto-migrate + seed
using (var scope = app.Services.CreateScope()) {
	var context = scope.ServiceProvider.GetRequiredService<PartyDbContext>();
	await context.Database.MigrateAsync();
	await DataSeeder.SeedAsync(context);
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
