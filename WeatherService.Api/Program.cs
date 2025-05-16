using WeatherService.Api;
using WeatherService.Application;
using WeatherService.Application.Models.Options;

var builder = WebApplication.CreateBuilder(args);

// Add MongoDB settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));

// Add OpenMeteo settings
builder.Services.Configure<OpenMeteoSettings>(builder.Configuration.GetSection("OpenMeteoApi"));

// Add Geocoding settings
builder.Services.Configure<GeocodingSettings>(builder.Configuration.GetSection("GeocodingApi"));

// Register services from WeatherService.Application
builder.Services.AddApplicationServices();

// Add controllers
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Error handling middleware
app.UseErrorHandling();

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
