using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Brands.Services;
using LaCasitaDeMiga.Features.Categories.Services;
using LaCasitaDeMiga.Features.Common.Cache.services;
using LaCasitaDeMiga.Features.Common.services.MailService;
using LaCasitaDeMiga.Features.DashBoard.Services;
using LaCasitaDeMiga.Features.Delivery.services;
using LaCasitaDeMiga.Features.GoogleGeoCoding.Services;
using LaCasitaDeMiga.Features.Orders.Services;
using LaCasitaDeMiga.Features.Payments.Services;
using LaCasitaDeMiga.Features.Products.Services;
using LaCasitaDeMiga.Features.Users.services;
using MercadoPago.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using System.Text;

// --- CONFIGURACIÓN INICIAL DE SERILOG (antes de crear el builder) ---
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try {
    Log.Information("Iniciando La Casita de Miga API...");

    var builder = WebApplication.CreateBuilder(args);

    // --- RECONFIGURAMOS SERILOG YA CON ACCESO A LA CONFIGURACIÓN (appsettings) ---
    builder.Host.UseSerilog((context, services, configuration) => {
        var sourceToken = context.Configuration["BetterStack:SourceToken"];
        var ingestingHost = context.Configuration["BetterStack:IngestingHost"];

        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console();

        if (!string.IsNullOrEmpty(sourceToken) && !string.IsNullOrEmpty(ingestingHost)) {
            configuration.WriteTo.BetterStack(
                sourceToken: sourceToken,
                betterStackEndpoint: $"https://{ingestingHost}",
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning

            );
        }
    });

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

    // Swagger con soporte de JWT
    builder.Services.AddSwaggerGen(options => {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ingresá el token JWT. No hace falta escribir 'Bearer', Swagger lo agrega solo."
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement {
            {
                new OpenApiSecurityScheme {
                    Reference = new OpenApiReference {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

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
    builder.Services.AddScoped<IPaymentService, PaymentServiceImpl>();
    builder.Services.AddSingleton<ICacheService, RedisCacheService>();

    // --- CONFIGURACIÓN DE REDIS (CACHÉ) ---
    var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
        ?? throw new InvalidOperationException("Falta configurar Redis:ConnectionString.");

    builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
        ConnectionMultiplexer.Connect(redisConnectionString));

    // --- CONFIGURACIÓN DE AUTENTICACIÓN JWT ---
    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("Falta configurar Jwt:Key.");

    builder.Services
        .AddAuthentication(options => {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options => {
            options.TokenValidationParameters = new TokenValidationParameters {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });

    builder.Services.AddAuthorization();

    // --- MANEJO GLOBAL DE EXCEPCIONES ---
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // --- CONFIGURACIÓN DEL PUERTO PARA RAILWAY/RENDER ---
    // Si existe la variable PORT, la usa; si no, usa el 8080 por defecto.
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://*:{port}");

    var app = builder.Build();

    app.UseSerilogRequestLogging();

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

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // --- AUTO-MIGRACIÓN PARA PRODUCCIÓN ---
    using (var scope = app.Services.CreateScope()) {
        var services = scope.ServiceProvider;
        try {
            var context = services.GetRequiredService<ApplicationDbContext>();
            // Esto le dice a PostgreSQL: "Si hay tablas o columnas nuevas en el código, crealas ya"
            await context.Database.MigrateAsync();
        } catch (Exception ex) {
            Log.Error(ex, "Ocurrió un error al aplicar las migraciones en la base de datos.");
        }
    }

    app.Run();
} catch (Exception ex) {
    Log.Fatal(ex, "La aplicación terminó inesperadamente durante el arranque.");
} finally {
    Log.CloseAndFlush();
}