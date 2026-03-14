using MongoDB.Driver;
using Audit.API.Application;
using Audit.API.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// MongoDB
var mongoClient = new MongoClient(
	builder.Configuration["MongoDB:ConnectionString"] ?? "mongodb://mongo:27017");
var mongoDatabase = mongoClient.GetDatabase(
	builder.Configuration["MongoDB:DatabaseName"] ?? "library_audit");
builder.Services.AddSingleton(mongoDatabase);

// Repository
builder.Services.AddScoped<IEventRepository, EventRepository>();

// Background services
builder.Services.AddHostedService<RabbitMqEventConsumer>();
builder.Services.AddHostedService<EventRetentionJob>();

// API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
	c.SwaggerDoc("v1", new() { Title = "Audit.API", Version = "v1" });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();
