using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Brands.Services;
using LaCasitaDeMiga.Features.Categories.Services;
using LaCasitaDeMiga.Features.Common.services.MailService;
using LaCasitaDeMiga.Features.DashBoard.Services;
using LaCasitaDeMiga.Features.Delivery.services;
using LaCasitaDeMiga.Features.GoogleGeoCoding.Services;
using LaCasitaDeMiga.Features.Orders.Services;
using LaCasitaDeMiga.Features.Payments.Services;
using LaCasitaDeMiga.Features.Products.Services;
using LaCasitaDeMiga.Features.Users.services;
using MercadoPago.Config;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURACIÓN DE BASE DE DATOS ---
// Lee automáticamente de appsettings.json en local y de las Variables de Entorno en Railway/Render
var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));


// --- CONFIGURACIÓN DE MERCADO PAGO ---
MercadoPagoConfig.AccessToken = builder.Configuration["MercadoPago:AccessToken"];

// --- CONFIGURACIÓN DE CORS ---
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", policy => {
        policy.WithOrigins(
            "https://www.lacasitademiga.com.ar",
            "https://lacasitademiga.com.ar",
            "http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
    options.AddPolicy("DevelopmentCors", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// --- INFRAESTRUCTURA Y SERVICIOS DEL CONTENEDOR ---
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- CONFIGURACIÓN DE CLIENTES HTTP Y LOGÍSTICA ---
builder.Services.AddHttpClient(); // Inicializa la fábrica de clientes HTTP global
builder.Services.AddHttpClient<IGoogleGeocodingService, GoogleGeocodingServiceImpl>(); // Vincula Google con capacidades HTTP
builder.Services.AddScoped<IDeliveryService, DeliveryServiceImpl>(); // Servicio calculador de 15Km

// --- SERVICIOS DE NEGOCIO ---
builder.Services.AddScoped<IBrandService, BrandServiceImpl>();
builder.Services.AddScoped<ICategoryService, CategoryServiceImpl>();
builder.Services.AddScoped<IOrderService, OrderServiceImpl>();
builder.Services.AddScoped<IProductService, ProductServiceImpl>();
builder.Services.AddScoped<IUserService, UserServiceImpl>();
builder.Services.AddScoped<IDashboardService, DashboardServiceImpl>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateServiceImpl>();
builder.Services.AddScoped<IPaymentService, PaymentServiceImpl>(); // ◄ nueva línea


// --- MANEJO GLOBAL DE EXCEPCIONES ---
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// --- CONFIGURACIÓN DEL PUERTO PARA RAILWAY ---
// Si existe la variable PORT (Railway), la usa; si no, usa el 8080 por defecto.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");



var app = builder.Build();




// --- PIPELINE DE SOLICITUDES HTTP (MIDDLEWARES) ---
app.UseExceptionHandler();

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- SELECCIÓN DINÁMICA DE CORS SEGÚN EL ENTORNO ---
if (app.Environment.IsDevelopment()) {
    app.UseCors("DevelopmentCors");
} else {
    app.UseCors("AllowAll");
}

app.UseAuthorization();
app.MapControllers();

// --- AUTO-MIGRACIÓN PARA PRODUCCIÓN ---
using (var scope = app.Services.CreateScope()) {
    var services = scope.ServiceProvider;
    try {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Esto le dice a PostgreSQL en Railway: "Si hay tablas o columnas nuevas en el código, crealas ya"
        await context.Database.MigrateAsync();
    } catch (Exception ex) {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al aplicar las migraciones en la base de datos.");
    }
}

app.Run();