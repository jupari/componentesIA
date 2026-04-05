using System.Threading.Channels;
using System.Text.Json.Serialization.Metadata;
using ComponentesIA.Application.Builders;
using ComponentesIA.Application.Interfaces;
using ComponentesIA.Application.UseCases;
using ComponentesIA.Infrastructure.AI;
using ComponentesIA.Infrastructure.Persistence;
using ComponentesIA.Infrastructure.Queue;
using ComponentesIA.Infrastructure.Repositories;
using ComponentesIA.Infrastructure.Storage;
using ComponentesIA.Middleware;
using ComponentesIA.Models.Settings;
using Google.Cloud.AIPlatform.V1;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configuración de CORS (permitir todo para desarrollo)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});

var configLog = new LoggerConfiguration()
    .WriteTo.File("logs//ApiLog-.log", rollingInterval: RollingInterval.Day)
    .MinimumLevel.Override("Default", LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .CreateLogger();
builder.Logging.AddSerilog(configLog);


// --- Configuración de Servicios ---

// 1. Añade servicios al contenedor de dependencias.
builder.Services.AddControllers().AddJsonOptions(static options =>
{
    options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
});

// 2. Configura Swagger/OpenAPI para la documentación de la API.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API de Componentes IA",
        Version = "v1",
        Description = "API para la extracción de datos y otros componentes de IA."
    });

    // JWT Bearer en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Ejemplo: 'Authorization: Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            new string[] {}
        }
    });
});

// 3. Configuración de Opciones (Options Pattern)
//    Carga la sección "Gemini" desde appsettings.json y la vincula a la clase GeminiSettings.
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
builder.Services.Configure<GoogleCloudSettings>(builder.Configuration.GetSection("GoogleCloud"));

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Políticas de autorización por rol y permiso
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    // Política general: requiere cualquier claim 'permission'
    options.AddPolicy("RequirePermission", policy => policy.RequireClaim("permission"));
    // Política específica: requiere el permiso 'upload_document'
    options.AddPolicy("UploadDocument", policy =>
        policy.RequireClaim("permission", "upload_document"));
});

// 4. Entity Framework + PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 5. Inyección de Dependencias — Repositorios
builder.Services.AddScoped<IExtractionTemplateRepository, ExtractionTemplateRepository>();
builder.Services.AddScoped<IDocumentBatchRepository, DocumentBatchRepository>();
builder.Services.AddScoped<IExtractionJobRepository, ExtractionJobRepository>();

// 6. Inyección de Dependencias — Servicios de Aplicación
builder.Services.AddScoped<IExtractionTemplateService, ExtractionTemplateService>();
builder.Services.AddScoped<IDocumentBatchService, DocumentBatchService>();
builder.Services.AddScoped<IExtractionJobProcessor, ExtractionJobProcessor>();
builder.Services.AddScoped<IExtractionValidationService, ExtractionValidationService>();
builder.Services.AddScoped<IPromptBuilderService, PromptBuilderService>();
// Auth
builder.Services.AddScoped<IAuthService, ComponentesIA.Services.AuthService>();

// 7. Inyección de Dependencias — Infraestructura
builder.Services.AddScoped<IDocumentStorageService, GcsDocumentStorageService>();
builder.Services.AddScoped<IAiExtractionService, ComponentesIA.Infrastructure.AI.GeminiExtractionService>();

// 8. Cola en memoria para procesamiento asíncrono de jobs
var jobChannel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });
builder.Services.AddSingleton(jobChannel);
builder.Services.AddSingleton<IJobDispatcher, InMemoryJobDispatcher>();
builder.Services.AddHostedService<JobProcessorBackgroundService>();

// 9. PayrollService (existente — se mantiene)
builder.Services.AddScoped<ComponentesIA.Services.Contracts.IPayrollService, ComponentesIA.Services.PayrollService>();

// 10. Registra el cliente del servicio de predicción de Vertex AI como Singleton.
builder.Services.AddSingleton<PredictionServiceClient>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<GeminiSettings>>().Value;
    var endpoint = $"{settings.Location}-aiplatform.googleapis.com";
    return new PredictionServiceClientBuilder { Endpoint = endpoint }.Build();
});

var app = builder.Build();

// Seed de roles, permisos y usuario admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// --- Configuración del Pipeline de Solicitudes HTTP ---

// 1. Configura Swagger en el entorno de desarrollo.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        options.RoutePrefix = string.Empty;
    });
}

// 2. Middleware de manejo de excepciones global.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 3. Redirección a HTTPS.
app.UseHttpsRedirection();

// Habilitar CORS
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// 4. Mapea los controladores para que el enrutamiento funcione.
app.MapControllers();

app.Run();
