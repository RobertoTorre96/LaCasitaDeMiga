using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Brands.Services;
using LaCasitaDeMiga.Features.Categories.Services;
using LaCasitaDeMiga.Features.Orders.Services;
using LaCasitaDeMiga.Features.Products.Services;
using LaCasitaDeMiga.Features.Users;
using LaCasitaDeMiga.Features.Users.services;
using Microsoft.AspNetCore.Authentication.JwtBearer; // <-- AGREGADO
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens; // <-- AGREGADO
using System.Text; // <-- AGREGADO

var builder = WebApplication.CreateBuilder(args);


// 1. REGISTRAR EL SERVICIO DE CORS (Antes del builder.Build())
builder.Services.AddCors(options => {
    options.AddPolicy("PermitirLaCasitaDeMiga", policy => {
        policy.WithOrigins(
                "https://www.lacasitademiga.com.ar", // 👈 Tu frontend real en producción
                "http://localhost:3000",             // 👈 Por si llegás a levantar el código del front local en React
                "http://localhost:4200"              // 👈 Por si usás Angular local
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


// --- CONFIGURACIÓN DE BASE DE DATOS ---
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// --- CONFIGURACIÓN DE AUTENTICACIÓN JWT (NUEVO) ---
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero
    };
});
// --------------------------------------------------

// Add services to the container.
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddScoped<IBrandService, BrandServiceImpl>();
builder.Services.AddScoped<ICategoryService, CategoryServiceImpl>();
builder.Services.AddScoped<IOrderService, OrderServiceImpl>();
builder.Services.AddScoped<IProductService, ProductServiceImpl>();
builder.Services.AddScoped<IUserService, UserServiceImpl>();

// Exceptions
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// --- CONFIGURACIÓN DEL PUERTO PARA RAILWAY ---
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddEndpointsApiExplorer();

// --- CONFIGURACIÓN DE SWAGGER CON JWT (MODIFICADO) ---
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo {
        Title = "La Casita de Miga API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
        Description = "Autenticación JWT usando el esquema Bearer. Ejemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
// -----------------------------------------------------

var app = builder.Build();
app.UseExceptionHandler();
app.UseCors("PermitirLaCasitaDeMiga");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- ORDEN CRÍTICO DE AUTENTICACIÓN Y AUTORIZACIÓN ---
app.UseAuthentication(); // 1. ¿Quién sos? (Lee el token) <-- AGREGADO
app.UseAuthorization();  // 2. ¿Tenés permiso? (Lee el rol)

app.MapControllers();

app.Run();