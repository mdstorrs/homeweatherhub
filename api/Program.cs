var builder = WebApplication.CreateBuilder(args);

// Secrets (e.g. the database connection string) live in a file that is not committed.
// It is published with the app, so it must exist locally before publishing.
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: false);

api.Model.MyData.ConnectionString = builder.Configuration.GetConnectionString("WeatherDb")
    ?? throw new InvalidOperationException(
        "Missing ConnectionStrings:WeatherDb. Create appsettings.Secrets.json (see appsettings.Secrets.example.json).");

// Add services to the container.

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
