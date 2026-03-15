using Lending.API.Application;
using Lending.API.Application.Interfaces;
using Lending.API.Domain;
using Lending.API.HttpClients;
using Lending.API.Infrastructure.Data;
using Lending.API.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Polly;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<LendingDbContext>(options =>
	options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services
builder.Services.AddScoped<IBorrowingService, BorrowingService>();

// Event publisher
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

// HTTP Clients with Polly
builder.Services.AddHttpClient<IPartyServiceClient, PartyServiceClient>(client => {
	client.BaseAddress = new Uri(builder.Configuration["Services:PartyApi"] ?? "http://party-api:8080");
})
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3,
	attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))))
.AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5,
	TimeSpan.FromSeconds(30)));

builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client => {
	client.BaseAddress = new Uri(builder.Configuration["Services:CatalogApi"] ?? "http://catalog-api:8080");
})
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3,
	attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt))))
.AddTransientHttpErrorPolicy(p => p.CircuitBreakerAsync(5,
	TimeSpan.FromSeconds(30)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope()) {
	var db = scope.ServiceProvider.GetRequiredService<LendingDbContext>();
	db.Database.EnsureCreated();
	await DataSeeder.SeedAsync(db);
}

app.Run();
