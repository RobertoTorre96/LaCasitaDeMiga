using ECommerceAPI.Data;
using ECommersAPI.Exceptions;
using ECommersAPI.Features.Brands.Services;
using ECommersAPI.Features.Categories.Services;
using ECommersAPI.Features.Orders.Services;
using ECommersAPI.Features.Products.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE BASE DE DATOS (NUEVA LOGICA NEON) ---
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");

// Declaramos y leemos la URL nativa de Neon desde las variables de entorno de Render
var envUrl = Environment.GetEnvironmentVariable("NEON_DATABASE_URL");
if (!string.IsNullOrEmpty(envUrl))
{
    connectionString = envUrl;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
// ---------------------------------------------------------------

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
app.UseAuthorization();
app.MapControllers();

app.Run();