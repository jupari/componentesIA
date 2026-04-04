using ComponentesIA.Middleware;
using ComponentesIA.Models.Settings;
using ComponentesIA.Services;
using ComponentesIA.Services.Contracts;
using Google.Cloud.AIPlatform.V1;
using Serilog;
using Serilog.Events;
using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization.Metadata;

var builder = WebApplication.CreateBuilder(args);

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
});

// 3. Configuración de Opciones (Options Pattern)
//    Carga la sección "Gemini" desde appsettings.json y la vincula a la clase GeminiSettings.
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));

// 4. Inyección de Dependencias Personalizadas.
builder.Services.AddScoped<IExtractionService, GeminiExtractionService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();

// 5. Registra el cliente del servicio de predicción de Vertex AI como Singleton.
//    Se recomienda usar un único cliente por la vida de la aplicación.
builder.Services.AddSingleton<PredictionServiceClient>(provider =>
{
    var settings = provider.GetRequiredService<IOptions<GeminiSettings>>().Value;
    var endpoint = $"{settings.Location}-aiplatform.googleapis.com";
    return new PredictionServiceClientBuilder { Endpoint = endpoint }.Build();
});

var app = builder.Build();

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

// 4. Mapea los controladores para que el enrutamiento funcione.
app.MapControllers();

app.Run();
