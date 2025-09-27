# Aspire Local Development Orchestration

> **⚠️ Important**: Aspire is **only used for local development**. Production deployments use Docker containers without Aspire components.

This guide covers the .NET Aspire orchestration setup for local development of the Clercq.It project.

## Project Structure

The Aspire orchestration consists of two projects:

### `Clercq.It.AppHost` - Orchestration Host
- **Purpose**: Defines and orchestrates all application services
- **Framework**: .NET 9 Console Application 
- **Features**:
  - PostgreSQL database with pgAdmin management UI
  - API service integration with automatic service discovery
  - Development dashboard with telemetry and logging

### `Clercq.It.ServiceDefaults` - Shared Configuration
- **Purpose**: Common configuration for observability and resilience
- **Framework**: .NET 9 Class Library
- **Features**:
  - OpenTelemetry integration for tracing and metrics
  - Health checks and service discovery
  - HTTP resilience patterns

## Services Defined

### PostgreSQL Database
- **Service Name**: `postgres`
- **Database Name**: `ClercqItDb`
- **Features**: 
  - Persistent data volume
  - pgAdmin web interface for database management
  - Automatic connection string injection

### API Service
- **Service Name**: `clercqit-api`
- **Project**: `ClercqIt.Api`
- **Features**:
  - Automatic service discovery
  - Database connection via Aspire service binding
  - Health checks and telemetry

### Next.js Frontend
- **Service Name**: `clercqit-web`
- **Project**: `ClercqIt.Web`
- **Features**:
  - Node.js application hosting
  - Automatic API service discovery
  - Environment variable injection
  - External HTTP endpoints for public access
  - Docker containerization support

## Running with Aspire

### Prerequisites
- .NET 9 SDK
- Docker Desktop (for PostgreSQL container)

### Start the Application
```bash
# Navigate to the AppHost project
cd src/Clercq.It.AppHost

# Run the orchestrated application
dotnet run
```

This will:
1. Start the Aspire Dashboard (typically at `http://localhost:15888`)
2. Launch PostgreSQL with pgAdmin
3. Start the API service with automatic database connection
4. Launch the Next.js frontend with automatic API service discovery
5. Provide comprehensive observability and monitoring

### Aspire Dashboard Features
- **Service Overview**: Monitor all running services
- **Logs**: Centralized logging from all services
- **Traces**: Distributed tracing across services
- **Metrics**: Performance metrics and health status
- **Configuration**: Service configuration and connection strings

## Development Benefits

### Automatic Service Discovery
- No manual connection string configuration needed
- Services automatically discover and connect to dependencies
- Environment-specific configuration handled automatically

### Enhanced Observability
- Distributed tracing out-of-the-box
- Structured logging aggregation
- Real-time metrics and health monitoring
- Performance profiling capabilities

### Simplified Development Workflow
- Single command starts entire application stack
- Integrated database management with pgAdmin
- Hot reload and development-optimized configurations
- Easy debugging with centralized logs and traces

## Configuration

### Database Connection
The API automatically receives the PostgreSQL connection string from Aspire:
- **Service Discovery**: Connection injected via `ConnectionStrings__ClercqItDb`
- **Fallback**: Traditional connection strings for non-Aspire environments
- **Migrations**: Run automatically on startup when using Aspire

### Service Registration
Services are configured in `Program.cs` of the AppHost:
```csharp
// PostgreSQL with management UI
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var database = postgres.AddDatabase("ClercqItDb");

// API service with database reference
var api = builder.AddProject<Projects.ClercqIt_Api>("clercqit-api")
    .WithReference(database);

// Next.js frontend with API reference
var web = builder.AddNodeApp("clercqit-web", "../ClercqIt.Web")
    .WithReference(api)
    .WithHttpEndpoint(env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();
```

### Service Defaults Integration
The API integrates service defaults for enhanced observability:
```csharp
// Add Aspire service defaults (telemetry, logging, health checks)
builder.AddServiceDefaults();

// Map health check endpoints
app.MapDefaultEndpoints();
```

## Local Development vs Production

### Local Development with Aspire
- **Purpose**: Simplifies local development with orchestration
- **Components**: AppHost orchestration, ServiceDefaults, and Aspire Dashboard
- **Database**: Containerized PostgreSQL with pgAdmin
- **Features**: Hot reload, centralized logging, distributed tracing

### Production Deployment
- **Architecture**: Single Docker container with nginx reverse proxy
- **Components**: Only API, frontend, and nginx (no Aspire components)
- **Database**: Managed PostgreSQL service (Scaleway, Azure, AWS, etc.)
- **Features**: Optimized for performance, security, and scalability

> **Key Point**: The `Clercq.It.AppHost` project and Aspire workload are **excluded from production Docker builds** to keep containers lightweight and focused on runtime requirements.

## Production Considerations

> **Note**: These considerations apply only if you choose to use Aspire for production (not recommended for this project).
- Aspire can generate Docker Compose files for production deployment
- Kubernetes manifests can be generated for cloud-native deployments
- Service mesh integration for advanced networking scenarios

### Configuration Management
- Environment-specific settings via Aspire configuration
- Secret management integration with Azure Key Vault or similar
- Feature flags and configuration hot-reload capabilities

### Monitoring and Alerting
- Integration with Application Insights or similar APM tools
- Custom metrics and business KPIs
- Automated alerting based on health checks and performance thresholds

## Troubleshooting

### Common Issues
1. **Port Conflicts**: Aspire Dashboard typically uses port 15888
2. **Docker Requirements**: PostgreSQL requires Docker Desktop to be running
3. **Network Connectivity**: Ensure Docker networks allow inter-container communication

### Debugging
- Check Aspire Dashboard logs for service startup issues
- Verify PostgreSQL container is running: `docker ps`
- Test API health endpoint: `https://localhost:7000/health`

### Performance
- Monitor service startup times in the Dashboard
- Check database connection pooling metrics
- Review HTTP client resilience patterns in action