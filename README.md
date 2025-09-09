# Azure Function IGGA - Migración a .NET 8

## 🚀 Descripción

Este proyecto es una migración completa de Azure Function IGGA desde versiones anteriores de .NET hacia .NET 8, implementando las mejores prácticas y las últimas características disponibles.

## ✨ Características Principales

### 🔧 Tecnologías Implementadas
- **.NET 8.0** - Framework principal
- **Azure Functions v4** - Runtime de Azure Functions
- **Entity Framework Core 8** - ORM con configuraciones avanzadas
- **AutoMapper 13** - Mappings automáticos entre entidades y DTOs
- **FluentValidation 11** - Validaciones robustas
- **Serilog 4** - Logging estructurado
- **System.Text.Json** - Serialización JSON de alta performance

### 🏗️ Arquitectura

```
├── Functions/                 # Azure Functions endpoints
├── Services/                  # Lógica de negocio
│   ├── Interfaces/           # Contratos de servicios
│   └── Implementations/      # Implementaciones concretas
├── Data/                     # Contexto de base de datos
├── Models/                   # Entidades y DTOs
├── Middlewares/              # Middlewares personalizados
├── Mappings/                 # Perfiles de AutoMapper
└── Program.cs               # Configuración de la aplicación
```

### 🔒 Características de Seguridad
- **Middleware de Autenticación** - Validación de tokens JWT/API Keys
- **Rate Limiting** - Control de frecuencia de solicitudes
- **CORS** - Configuración de políticas de origen cruzado
- **Auditoría Automática** - Registro de cambios en entidades

### 📊 Funcionalidades de Negocio
- **Gestión de Usuarios** - CRUD completo con roles y permisos
- **Procesamiento de Datos** - Queue triggers y background jobs
- **Sistema de Notificaciones** - Notificaciones en tiempo real
- **Generación de Reportes** - PDF, Excel y visualizaciones
- **Almacenamiento de Archivos** - Azure Blob Storage integration

## 🛠️ Configuración del Entorno

### Prerrequisitos
- .NET 8.0 SDK
- Azure Functions Core Tools v4
- SQL Server (LocalDB para desarrollo)
- Azure Storage Emulator
- Visual Studio 2022 o VS Code

### Instalación

1. **Clonar el repositorio:**
```bash
git clone https://github.com/EchoNovaTech-v1/azure-function-igga-dotnet8.git
cd azure-function-igga-dotnet8
```

2. **Restaurar paquetes:**
```bash
dotnet restore
```

3. **Configurar base de datos:**
```bash
dotnet ef database update
```

4. **Ejecutar en modo desarrollo:**
```bash
func start
```

### Variables de Entorno

Configurar las siguientes variables en `local.settings.json`:

```json
{
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "Server=(localdb)\\mssqllocaldb;Database=IggaDatabase;Trusted_Connection=true;",
    "APPINSIGHTS_INSTRUMENTATIONKEY": "tu-instrumentation-key",
    "KeyVaultUrl": "https://tu-keyvault.vault.azure.net/",
    "ApiKey": "tu-api-key-secreta"
  }
}
```

## 📚 API Endpoints

### Usuarios
- `GET /api/users` - Listar usuarios
- `GET /api/users/{id}` - Obtener usuario por ID
- `POST /api/users` - Crear usuario
- `PUT /api/users/{id}` - Actualizar usuario
- `DELETE /api/users/{id}` - Eliminar usuario
- `POST /api/users/{id}/activate` - Activar usuario
- `POST /api/users/{id}/deactivate` - Desactivar usuario

### Procesamiento de Datos
- `POST /api/data/process` - Procesar datos
- `POST /api/reports/generate` - Generar reportes
- `GET /api/health` - Health check

### Autenticación
Incluir en headers:
```
Authorization: Bearer {jwt-token}
# o
Authorization: ApiKey {api-key}
```

## 🔄 Migraciones desde Versiones Anteriores

### Cambios Principales en .NET 8

1. **Worker Model**: Migración de in-process a isolated worker model
2. **Program.cs**: Configuración moderna con top-level statements
3. **Dependency Injection**: Uso del nuevo HostBuilder
4. **JSON**: System.Text.Json en lugar de Newtonsoft.Json
5. **Records**: Uso de records para DTOs inmutables

### Proceso de Migración

1. **Actualizar Target Framework:**
```xml
<TargetFramework>net8.0</TargetFramework>
<AzureFunctionsVersion>v4</AzureFunctionsVersion>
```

2. **Actualizar Paquetes NuGet:**
```xml
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.21.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Sdk" Version="1.16.4" />
```

3. **Migrar Functions:**
```csharp
// Antes (.NET Framework/Core)
[FunctionName("GetUser")]
public static async Task<IActionResult> GetUser(
    [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)

// Después (.NET 8)
[Function("GetUser")]
public async Task<HttpResponseData> GetUser(
    [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
```

4. **Actualizar Program.cs:**
```csharp
// Configuración moderna con HostBuilder
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        // Configurar servicios
    })
    .Build();
```

## 🧪 Testing

### Ejecutar Tests
```bash
dotnet test
```

### Estructura de Tests
```
├── Tests/
│   ├── Unit/              # Tests unitarios
│   ├── Integration/       # Tests de integración
│   └── Performance/       # Tests de rendimiento
```

## 📈 Monitoreo y Logging

### Application Insights
- Telemetría automática
- Métricas personalizadas
- Rastreo de dependencias
- Alertas configurables

### Serilog
- Logging estructurado
- Múltiples sinks (Console, ApplicationInsights)
- Contexto enriquecido
- Filtrado avanzado

## 🚀 Deployment

### Azure DevOps Pipeline
```yaml
# azure-pipelines.yml
trigger:
- main
- migration-dotnet8

pool:
  vmImage: 'windows-latest'

steps:
- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Build project'
  inputs:
    command: 'build'
    configuration: 'Release'

- task: DotNetCoreCLI@2
  displayName: 'Publish project'
  inputs:
    command: 'publish'
    publishWebProjects: false
    projects: '**/*.csproj'
    arguments: '--configuration Release --output $(Build.ArtifactStagingDirectory)'
```

### GitHub Actions
```yaml
# .github/workflows/deploy.yml
name: Deploy to Azure Functions

on:
  push:
    branches: [ main, migration-dotnet8 ]

jobs:
  deploy:
    runs-on: windows-latest
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore
    
    - name: Publish
      run: dotnet publish -c Release -o ./output
    
    - name: Deploy to Azure Functions
      uses: Azure/functions-action@v1
      with:
        app-name: ${{ secrets.AZURE_FUNCTIONAPP_NAME }}
        package: './output'
        publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}
```

## 🔧 Troubleshooting

### Problemas Comunes

1. **Error de Connection String:**
   - Verificar `local.settings.json`
   - Comprobar acceso a SQL Server

2. **Problemas de Autenticación:**
   - Validar tokens JWT
   - Verificar configuración de Azure AD

3. **Issues de Performance:**
   - Revisar Application Insights
   - Optimizar queries de EF Core

## 📋 Changelog

### v2.0.0 (Migración .NET 8)
- ✅ Migración completa a .NET 8
- ✅ Azure Functions v4 Worker Model
- ✅ Entity Framework Core 8
- ✅ Middlewares personalizados
- ✅ Auditoría automática
- ✅ Sistema de notificaciones
- ✅ Health checks avanzados

### v1.x.x (Versión Anterior)
- Implementación en .NET Framework/Core anterior
- In-process model

## 🤝 Contribución

1. Fork el proyecto
2. Crear feature branch (`git checkout -b feature/nueva-funcionalidad`)
3. Commit cambios (`git commit -am 'Agregar nueva funcionalidad'`)
4. Push a la rama (`git push origin feature/nueva-funcionalidad`)
5. Crear Pull Request

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para detalles.

## 👥 Equipo

- **Luis Gabriel Ahumada** - *Arquitecto Principal* - [@luisgabrielahumada](https://github.com/luisgabrielahumada)
- **EchoNova Tech** - *Organización* - [@EchoNovaTech-v1](https://github.com/EchoNovaTech-v1)

## 📞 Soporte

Para soporte técnico o preguntas:
- 📧 Email: support@echonovatech.com
- 🐛 Issues: [GitHub Issues](https://github.com/EchoNovaTech-v1/azure-function-igga-dotnet8/issues)
- 📖 Documentación: [Wiki del Proyecto](https://github.com/EchoNovaTech-v1/azure-function-igga-dotnet8/wiki)