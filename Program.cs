using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using ProjectsDonetskWaterHope.Data;
using ProjectsDonetskWaterHope.Models;
using ProjectsDonetskWaterHope.Services; // Для TokenService та EmailQueue
using ProjectsDonetskWaterHope.Endpoints; // Для MapUserEndpoints
using Microsoft.AspNetCore.Authentication.JwtBearer; // Для налаштування JWT
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// --- 1. ПІДКЛЮЧЕННЯ ДО БАЗИ ДАНИХ ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Вирішення проблеми циклічних посилань JSON
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// --- 2. РЕЄСТРАЦІЯ ВЛАСНИХ СЕРВІСІВ (ВАЖЛИВО!) ---
builder.Services.AddScoped<TokenService>(); // Сервіс токенів

// --- 3. НАЛАШТУВАННЯ АУТЕНТИФІКАЦІЇ (JWT) ---
// Без цього сервер не зможе перевіряти токени
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization(); // Додаємо авторизацію
builder.Services.AddScoped<LoggerService>();
// --- 4. НАЛАШТУВАННЯ SWAGGER ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DonetskWaterHope API",
        Version = "v1",
        Description = "API для системи моніторингу води (Lab 2)"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Введіть JWT токен"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// --- 5. CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

var app = builder.Build();

// --- 6. PIPELINE ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DonetskWaterHope API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

// ВАЖЛИВО: Порядок має значення!
app.UseAuthentication(); // 1. Хто ти?
app.UseAuthorization();  // 2. Чи можна тобі сюди?
app.MapUserEndpoints();
app.MapTariffEndpoints();
app.MapDeviceEndpoints();
app.MapSupportTicketEndpoints();
app.MapAlertEndpoints();
app.MapConsumptionEndpoints();
// --- БЛОК SEEDING (Додайте це) ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Створить БД, якщо її немає (або виконає міграції)
    db.Database.EnsureCreated();

    // Запускаємо наш сідер
    ProjectsDonetskWaterHope.Data.DbInitializer.Seed(db);
}
// --------------------------------

// ... app.UseSwagger(); ...
app.Run();