# GitForest.AppHost

.NET Aspire orchestration host for git-forest distributed services.

## Overview

This is the Aspire AppHost that orchestrates all git-forest distributed services including Orleans clustering and the web application. It provides a unified development and deployment experience with built-in observability.

## Running

```bash
cd src/GitForest.AppHost
dotnet run
```

## What Gets Started

When you run the AppHost, it starts:

1. **Orleans Cluster**: Development clustering for distributed grain execution
2. **GitForest.Web**: Web UI with Orleans integration
3. **Aspire Dashboard**: Monitoring and observability dashboard

## Accessing Services

- **Aspire Dashboard**: `http://localhost:15888` (or as shown in console output)
- **GitForest.Web**: `http://localhost:5000` (or as configured)

## Configuration

The AppHost configuration is in `Program.cs`. Key configuration:

- Orleans clustering mode (development/production)
- Service dependencies and references
- Resource allocations

## Development vs Production

### Development
- Uses localhost clustering
- In-memory grain storage
- Single-node deployment

### Production
- Configure Azure Storage or other clustering provider
- Persistent grain storage
- Multi-node deployment with load balancing

## Observability

The Aspire dashboard provides:
- Service health and status
- Logs aggregation
- Distributed tracing
- Metrics and performance data

## Next Steps

After starting the AppHost:
1. Open the Aspire dashboard to see running services
2. Navigate to GitForest.Web to use the UI
3. Monitor logs and metrics in the dashboard
