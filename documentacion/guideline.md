Perfecto. Entonces te propongo un backend **100% orientado a procesamiento asíncrono por lotes**, que te permita subir varios documentos, aplicar una configuración de extracción y devolver un **JSON estructurado por documento**. Para .NET en GCP, una base sólida es: **Cloud Storage** para archivos, **Vertex AI/Gemini** para extracción, y una capa de trabajo asíncrono con **Cloud Tasks** o **Pub/Sub**, ambos con SDK oficial para .NET. Google además publica librerías oficiales para Vertex AI, Document AI, Cloud Tasks y Pub/Sub en .NET. ([Google Cloud Documentation][1])

# 1. Decisión de arquitectura asíncrona

Para tu caso, la mejor secuencia es esta:

1. Angular sube uno o varios archivos.
2. La API .NET registra el lote y guarda archivos en storage.
3. La API devuelve rápido un `batchId`.
4. Un proceso en segundo plano toma cada documento.
5. Se construye el prompt según la configuración.
6. Gemini procesa el documento y responde JSON.
7. El backend valida, normaliza y persiste resultado.
8. Angular consulta el estado del lote o recibe notificaciones.

Esto evita timeouts HTTP, soporta PDFs largos y te deja reintentar solo los documentos fallidos.

---

# 2. Qué usar para la cola asíncrona

Tienes dos opciones buenas en GCP:

## Opción recomendada para empezar: Cloud Tasks

Cloud Tasks está pensado para ejecutar trabajo asíncrono fuera del request original y despacharlo a endpoints HTTP administrados por tu backend. Es muy cómodo cuando tu flujo es “crear tarea → llamar endpoint worker → procesar un documento”. ([Google Cloud Documentation][2])

## Opción recomendada para escalar eventos: Pub/Sub

Pub/Sub es mensajería asíncrona escalable y desacopla el sistema que publica del sistema que procesa. Es útil si luego vas a tener varios consumidores, eventos, reprocesos, auditoría separada o pipelines más complejos. Pub/Sub trabaja con entrega al menos una vez. ([Google Cloud Documentation][3])

## Mi recomendación práctica

* **Fase inicial:** Cloud Tasks
* **Fase de crecimiento:** Pub/Sub

Porque tu caso hoy no necesita una arquitectura event-driven compleja. Necesita **control operativo fino por documento**.

---

# 3. Arquitectura backend propuesta

## Componentes

### Angular

* carga múltiple de archivos
* consulta estado del lote
* visualización del JSON extraído
* validación manual de campos

### API .NET

* recibe archivos
* registra lote y documentos
* sube archivos a Cloud Storage
* crea tareas asíncronas
* expone endpoints de consulta

### Worker .NET

* recibe `documentId` o `jobId`
* obtiene configuración de extracción
* construye prompt dinámico
* invoca Gemini
* parsea JSON
* valida y normaliza
* guarda resultados

### Google Cloud Storage

* almacenamiento temporal o persistente del PDF/imagen

### Vertex AI / Gemini

* extracción multimodal desde PDF o imagen
* salida en JSON

### Base de datos

* plantillas
* campos configurables
* lotes
* jobs
* resultados
* auditoría

---

# 4. Diseño de flujo asíncrono

## Flujo de carga

### Endpoint

`POST /api/document-batches`

### Acción

* recibe `templateId`
* recibe múltiples archivos
* crea `batch`
* crea `documentFiles`
* sube cada archivo a bucket
* crea un `job` por archivo
* encola cada job

### Respuesta

```json
{
  "batchId": "b7f5b4e8-2d2b-4e49-8f42-3c8b814f1d21",
  "status": "Pending",
  "totalDocuments": 5,
  "jobs": [
    {
      "jobId": "6e4d2b57-7e3f-4b5a-8f0a-6a08b0d25f88",
      "fileName": "certificado1.pdf",
      "status": "Pending"
    }
  ]
}
```

## Flujo del worker

Por cada job:

1. cambia estado a `Processing`
2. obtiene archivo desde GCS
3. lee la plantilla
4. arma prompt
5. llama a Gemini
6. recibe JSON
7. valida y normaliza
8. guarda resultado
9. cambia estado a `Completed` o `Failed`

---

# 5. Modelo de dominio recomendado

## Entidades principales

### `ExtractionTemplate`

Define el tipo de extracción.

Campos sugeridos:

* `Id`
* `Name`
* `Code`
* `Description`
* `AiProvider`
* `ModelName`
* `PromptStrategy`
* `IsActive`

### `ExtractionField`

Campos configurables.

* `Id`
* `TemplateId`
* `FieldKey`
* `Label`
* `Description`
* `DataType`
* `IsRequired`
* `AliasesJson`
* `ValidationRegex`
* `ExampleValue`
* `Order`
* `NormalizeRule`
* `ConfidenceThreshold`

### `DocumentBatch`

Lote de carga.

* `Id`
* `TemplateId`
* `UploadedBy`
* `Status`
* `TotalDocuments`
* `ProcessedDocuments`
* `FailedDocuments`
* `CreatedAt`

### `DocumentFile`

Archivo cargado.

* `Id`
* `BatchId`
* `OriginalFileName`
* `ContentType`
* `StoragePath`
* `Sha256`
* `PageCount`
* `Status`

### `ExtractionJob`

Proceso por documento.

* `Id`
* `DocumentFileId`
* `TemplateId`
* `Status`
* `Attempts`
* `StartedAt`
* `FinishedAt`
* `Provider`
* `Model`
* `PromptVersion`
* `ErrorMessage`

### `ExtractionResult`

Resultado general del job.

* `Id`
* `JobId`
* `RawResponse`
* `NormalizedJson`
* `ValidationStatus`
* `ConfidenceScore`

### `ExtractionFieldResult`

Resultado por campo.

* `Id`
* `ResultId`
* `FieldKey`
* `RawValue`
* `NormalizedValue`
* `Confidence`
* `IsValid`
* `ValidationMessage`
* `PageNumber`
* `BoundingBoxJson`

---

# 6. Cómo hacer el prompt configurable

La pieza clave es que el backend no tenga campos “quemados” en código.

## La plantilla debe guardar:

* nombre del documento
* campos
* aliases
* reglas
* instrucciones adicionales
* esquema JSON esperado

## Ejemplo de configuración de un campo

```json
{
  "fieldKey": "nit",
  "label": "NIT",
  "dataType": "string",
  "isRequired": true,
  "aliases": ["NIT", "Número de identificación tributaria"],
  "validationRegex": "^[0-9\\-\\.]+$",
  "normalizeRule": "trim|uppercase"
}
```

## Prompt builder

El backend genera automáticamente algo como:

```text
Analiza este documento legal colombiano y extrae los siguientes campos.

Campos requeridos:
- nit (string)
- razon_social (string)
- representantes_legales (array)
- capital (number)

Reglas:
- No inventes valores.
- Si no encuentras un campo, devuelve null.
- Responde únicamente JSON válido.
- Incluye confidence por campo entre 0 y 1.
- Capital debe ser un número sin símbolos monetarios.
```

## JSON objetivo

```json
{
  "documentType": "camara_comercio_persona_juridica",
  "fields": {
    "nit": null,
    "razon_social": null,
    "capital": null,
    "representantes_legales": []
  },
  "fieldConfidence": {
    "nit": 0,
    "razon_social": 0,
    "capital": 0,
    "representantes_legales": 0
  }
}
```

---

# 7. Contrato de respuesta del backend

Yo te recomiendo no devolver solo el JSON de negocio.
Devuelve también metadatos operativos.

## Ejemplo

```json
{
  "jobId": "6e4d2b57-7e3f-4b5a-8f0a-6a08b0d25f88",
  "status": "Completed",
  "document": {
    "fileName": "certificado1.pdf",
    "pages": 6
  },
  "result": {
    "documentType": "camara_comercio_persona_juridica",
    "fields": {
      "nit": "900123456-7",
      "razon_social": "EMPRESA DEMO SAS",
      "capital": 50000000,
      "representantes_legales": [
        {
          "nombre": "JUAN PEREZ",
          "cargo": "Representante Legal Principal"
        }
      ]
    },
    "fieldConfidence": {
      "nit": 0.97,
      "razon_social": 0.96,
      "capital": 0.84,
      "representantes_legales": 0.89
    }
  },
  "warnings": [],
  "processedAt": "2026-04-04T16:15:00Z"
}
```

---

# 8. Endpoints backend recomendados

## Plantillas

* `POST /api/extraction/templates`
* `GET /api/extraction/templates`
* `GET /api/extraction/templates/{id}`
* `PUT /api/extraction/templates/{id}`

## Lotes

* `POST /api/extraction/batches`
* `GET /api/extraction/batches/{batchId}`
* `GET /api/extraction/batches/{batchId}/jobs`

## Jobs

* `GET /api/extraction/jobs/{jobId}`
* `GET /api/extraction/jobs/{jobId}/result`
* `POST /api/extraction/jobs/{jobId}/retry`

## Auditoría

* `GET /api/extraction/jobs/{jobId}/audit`

---

# 9. Estados que sí debes manejar

## BatchStatus

* `Pending`
* `Processing`
* `Completed`
* `CompletedWithErrors`
* `Failed`

## JobStatus

* `Pending`
* `Queued`
* `Processing`
* `Completed`
* `Failed`
* `Retrying`
* `Cancelled`

Esto te permite que Angular pinte progreso real:

* 10 de 20 procesados
* 2 fallidos
* 8 pendientes

---

# 10. Estructura .NET sugerida

```text
src/
  Api/
    Controllers/
    Contracts/
    Middlewares/

  Application/
    DTOs/
    Interfaces/
    UseCases/
    Validators/
    Builders/

  Domain/
    Entities/
    Enums/
    ValueObjects/

  Infrastructure/
    Persistence/
    Repositories/
    Storage/
    AI/
    Queue/
    Logging/
```

---

# 11. Servicios que debes implementar

## `IDocumentStorageService`

Sube y obtiene archivos de Cloud Storage.

Métodos:

* `UploadAsync`
* `DownloadAsync`
* `GetSignedUrlAsync`

## `IPromptBuilderService`

Construye prompt + schema.

Métodos:

* `BuildSystemPrompt`
* `BuildUserPrompt`
* `BuildExpectedJsonSchema`

## `IAiExtractionService`

Abstracción del motor IA.

Métodos:

* `ExtractAsync`

Implementaciones:

* `GeminiExtractionService`
* `DocumentAiExtractionService`

## `IExtractionValidationService`

Valida y normaliza JSON.

Métodos:

* `ParseAsync`
* `ValidateAsync`
* `NormalizeAsync`

## `IJobDispatcher`

Publica a Cloud Tasks o Pub/Sub.

Métodos:

* `DispatchAsync`

## `IExtractionJobProcessor`

Procesa un job completo.

Métodos:

* `ProcessAsync`

---

# 12. Recomendación puntual sobre Gemini en .NET

Google documenta librerías cliente para Vertex AI y también el **Google Gen AI SDK** para trabajar con Gemini en Vertex AI. Además, en enero de 2026 Google anunció una librería para integrar Vertex AI con `Microsoft.Extensions.AI` en .NET, aunque esa librería fue anunciada como beta/pre-release. Para un proyecto productivo, mi recomendación es comenzar con las librerías oficiales estables/documentadas de Google y encapsular el proveedor detrás de tu interfaz `IAiExtractionService`. ([Google Cloud Documentation][1])

Eso te deja dos ventajas:

* puedes cambiar de Gemini a Document AI sin romper tu aplicación,
* puedes cambiar el modelo sin tocar controladores ni casos de uso.

---

# 13. Estrategia híbrida recomendada

Yo haría esto:

## Primer procesamiento

* usar Gemini en Vertex AI

## Fallback automático

Si ocurre alguno de estos casos:

* JSON inválido
* demasiados `null`
* baja confianza
* documento escaneado muy malo
* estructura tabular compleja

entonces:

* reintento con prompt más estricto
* o fallback a Document AI

Google también mantiene librerías oficiales para Document AI en .NET, así que este segundo camino queda perfectamente integrable en la misma arquitectura. ([Google Cloud Documentation][4])

---

# 14. Validación y normalización

No confíes ciegamente en el JSON del modelo.

## Debes validar:

* JSON bien formado
* todos los campos requeridos
* tipos correctos
* regex por campo
* rangos y reglas de negocio

## Debes normalizar:

* moneda a decimal
* fechas a ISO 8601
* quitar saltos de línea innecesarios
* limpiar espacios dobles
* unificar mayúsculas/minúsculas según regla

## Ejemplos

* `" $ 50.000.000 "` → `50000000`
* `"900.123.456-7"` → `"900123456-7"` o el formato que definas
* `"representante legal principal"` → `"Representante Legal Principal"`

---

# 15. Diseño de reintentos

Todo job fallido no debe reprocesarse infinito.

## Política recomendada

* máximo 3 intentos
* backoff exponencial
* guardar motivo del error

## Tipos de fallo

### Reintentables

* timeout
* error temporal de red
* rate limit
* servicio IA no disponible

### No reintentables

* archivo corrupto
* formato no soportado
* plantilla inexistente
* JSON persistente inválido tras corrección

---

# 16. Seguridad y auditoría

## Seguridad

* validar MIME y tamaño
* no confiar en extensión del archivo
* usar bucket privado
* exponer solo signed URLs si necesitas visor externo
* sanitizar nombres de archivo

## Auditoría

Debes guardar:

* prompt usado
* versión del prompt
* modelo usado
* response crudo
* JSON normalizado
* duración
* número de intentos
* usuario que cargó
* fecha de procesamiento

Eso te sirve para soporte, debugging y mejora continua.

---

# 17. Monitoreo que conviene desde el día 1

Métricas mínimas:

* documentos procesados por día
* porcentaje de éxito
* tiempo promedio por documento
* costo estimado por lote
* campos con más fallos
* plantillas con más errores

---

# 18. Plan detallado de implementación backend

## Fase A. Núcleo del dominio

Construir:

* entidades
* enums
* repositorios
* migraciones
* CRUD de plantillas

**Salida:** backend con configuración dinámica lista.

## Fase B. Gestión documental

Construir:

* endpoint de carga múltiple
* integración con Cloud Storage
* registro de lotes y documentos
* hash y metadata

**Salida:** ya puedes subir varios PDFs y registrarlos.

## Fase C. Orquestación asíncrona

Construir:

* dispatcher
* cola con Cloud Tasks
* worker endpoint
* estados de batch y job

**Salida:** procesamiento desacoplado del request HTTP.

## Fase D. Motor de prompts

Construir:

* `PromptBuilder`
* schema generator
* versionado de prompts

**Salida:** el sistema arma el prompt solo con base en configuración.

## Fase E. Integración IA

Construir:

* `GeminiExtractionService`
* contrato request/response
* parser de salida

**Salida:** extracción real desde documentos.

## Fase F. Validación y normalización

Construir:

* validadores por tipo
* normalizadores
* almacenamiento de field results

**Salida:** JSON limpio, consistente y trazable.

## Fase G. Reintentos y fallback

Construir:

* retry policy
* fallback a Document AI
* control de errores

**Salida:** pipeline robusto.

## Fase H. Observabilidad

Construir:

* logs estructurados
* métricas
* endpoint de auditoría

**Salida:** operación y soporte reales.

---

# 19. Mi recomendación técnica final

Para arrancar bien, yo haría este stack:

* **ASP.NET Core 8**
* **EF Core**
* **SQL Server o PostgreSQL**
* **Google Cloud Storage**
* **Vertex AI con Gemini como motor principal**
* **Cloud Tasks para el procesamiento asíncrono**
* **Document AI como fallback**
* **plantillas configurables**
* **JSON versionado**
* **resultado por campo con confidence**

---

# 20. Siguiente paso recomendado

El siguiente paso ya no debería ser más teoría.
Debería ser construir esto en orden:

1. **entidades y tablas**
2. **DTOs**
3. **endpoints**
4. **servicio de almacenamiento**
5. **dispatcher asíncrono**
6. **procesador del job**
7. **integración Gemini**