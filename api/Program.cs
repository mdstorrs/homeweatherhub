var builder = WebApplication.CreateBuilder(args);

// Secrets (e.g. the database connection string) live in a file that is not committed.
// It is published with the app, so it must exist locally before publishing.
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: false);
// Per host: e.g. appsettings.Secrets.SmartASP.json when ASPNETCORE_ENVIRONMENT=SmartASP (set by that publish profile).
// Required on named hosts, so a missing file stops the app instead of silently using another host's database.
bool namedHost = !builder.Environment.IsDevelopment() && !builder.Environment.IsProduction();
builder.Configuration.AddJsonFile($"appsettings.Secrets.{builder.Environment.EnvironmentName}.json", optional: !namedHost, reloadOnChange: false);

// All stored times are in this zone, regardless of where the server is (see AppTime).
api.Model.AppTime.Configure(builder.Configuration["AppTimeZone"]);

api.Model.MyData.ConnectionString = builder.Configuration.GetConnectionString("WeatherDb")
    ?? throw new InvalidOperationException(
        $"Missing ConnectionStrings:WeatherDb. Create appsettings.Secrets.json or appsettings.Secrets.{builder.Environment.EnvironmentName}.json (see appsettings.Secrets.example.json).");

// Add services to the container.

// Database clean-up every few hours (7 days of readings, 30 days of log) where migration 003 exists.
builder.Services.AddHostedService<api.Model.MaintenanceService>();

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

// 🔹 Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", // Changed policy name to reflect its purpose
        policy =>
        {
            policy.AllowAnyOrigin() // Allows any origin
                   .AllowAnyMethod() // Allows any HTTP method
                   .AllowAnyHeader(); // Allows any HTTP header
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseCors("AllowAll"); // Apply the "AllowAll" policy
app.UseRouting();

//app.UseHttpsRedirection();
app.UseDefaultFiles(); // Enables default file serving (like index.html)
app.UseAuthorization();

app.MapControllers();

app.Run();
