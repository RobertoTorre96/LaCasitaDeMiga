using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Brands.Services;
using LaCasitaDeMiga.Features.Categories.Services;
using LaCasitaDeMiga.Features.Orders.Services;
using LaCasitaDeMiga.Features.Products.Services;
using LaCasitaDeMiga.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE BASE DE DATOS ---
// Lee automáticamente de appsettings.json en local y de las Variables de Entorno en Render
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
// --------------------------------------
// --- 1. CONFIGURACIÓN DE CORS (NUEVO) ---
// --- CONFIGURACIÓN DE CORS RESTRINGIDA A TU FRONTEND ---
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.WithOrigins(
            "https://www.lacasitademiga.com.ar"
            , "https://lacasitademiga.com.ar",
            "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


// Add services to the container.
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddScoped<IBrandService, BrandServiceImpl>();
builder.Services.AddScoped<ICategoryService, CategoryServiceImpl>();
builder.Services.AddScoped<IOrderService, OrderServiceImpl>();
builder.Services.AddScoped<IProductService, ProductServiceImpl>();

// Exceptions
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();

// --- CONFIGURACIÓN DEL PUERTO PARA RAILWAY ---
// Si existe la variable PORT (Railway), la usa; si no, usa el 8080 por defecto en producción.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();