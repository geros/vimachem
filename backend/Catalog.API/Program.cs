using Catalog.API.Application.Interfaces;
using Catalog.API.Application.Services;
using Catalog.API.Application.Validators;
using Catalog.API.HttpClients;
using Catalog.API.Infrastructure.Messaging;
using Catalog.API.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core
builder.Services.AddDbContext<CatalogDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateBookValidator>();

// HTTP Client with Polly
builder.Services.AddHttpClient<IPartyServiceClient, PartyServiceClient>(client => {
	client.BaseAddress = new Uri(builder.Configuration["Services:PartyApi"]
		?? "http://party-api:8080");
})
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3,
	attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))))
.AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5,
	TimeSpan.FromSeconds(30)));

// Services
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

// Event Publisher (RabbitMQ)
builder.Services.AddSingleton<IEventPublisher>(
	new RabbitMqEventPublisher(builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Seed data
using (var scope = app.Services.CreateScope()) {
	var context = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
	await context.Database.MigrateAsync();
	await DataSeeder.SeedAsync(context);
}

app.Run();
