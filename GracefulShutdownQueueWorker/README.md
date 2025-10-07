# Graceful Shutdown Queue Worker

A .NET worker service that demonstrates graceful shutdown patterns when processing Azure Storage Queue messages in Kubernetes. This project shows how to handle SIGTERM signals properly to ensure in-flight messages are processed before container termination.

## Features

- **Graceful Shutdown**: Handles SIGTERM signals to complete message processing
- **Azure Storage Queue**: Processes messages from Azure Storage Queue
- **Serilog Logging**: Structured logging with console and file outputs
- **Kubernetes Ready**: Includes deployment manifests and persistent volume configuration
- **Docker Support**: Multi-stage Dockerfile for optimized container images

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) with Kubernetes enabled
- [kubectl](https://kubernetes.io/docs/tasks/tools/) CLI tool
- Azure Storage Account (for queue operations)

## Quick Start

### 1. Clone and Build

```bash
git clone <repository-url>
cd GracefulShutdownQueueWorker
dotnet build
```

### 2. Set Environment Variables

```bash
# Set your Azure Storage connection string
export AzureWebJobsStorage="DefaultEndpointsProtocol=https;AccountName=<account>;AccountKey=<key>;EndpointSuffix=core.windows.net"
```

### 3. Run Locally

```bash
dotnet run
```

## Docker Deployment

### Create Docker Image with Tag

```bash
# Build the Docker image
docker build -t graceful-queue-worker:latest .

# Tag for Docker Hub (replace with your username)
docker tag graceful-queue-worker:latest rmanimaran/graceful-queue-worker:latest
```

### Push Image to Docker Hub

```bash
# Login to Docker Hub
docker login

# Push the image
docker push rmanimaran/graceful-queue-worker:latest
```

### Test the SIGTERM in Docker

```bash
# Run container with environment variable
docker run -e AzureWebJobsStorage="<your-connection-string>" graceful-queue-worker:latest

# In another terminal, test graceful shutdown
docker ps  # Get container ID
docker stop <container-id>  # Sends SIGTERM
```

## Kubernetes Deployment

### 1. Create Namespace

```bash
kubectl apply -f K8s/namespace.yaml
```

### 2. Create Secret

```bash
# Encode your connection string to base64
echo -n "<your-azure-storage-connection-string>" | base64

# Update K8s/secret.yaml with the encoded value
# Then apply:
kubectl apply -f K8s/secret.yaml -n gracefulshutdown
```

### 3. Setup Persistent Volume (Docker Desktop)

```bash
# For Docker Desktop on Windows/Mac
kubectl apply -f K8s/pv-docker-desktop.yaml
kubectl apply -f K8s/pvc-docker-desktop.yaml
```

### 4. Apply Deployment

```bash
kubectl apply -f K8s/deployment.yaml
```

### 5. Validate the Queue Processing and Test Graceful Shutdown

```bash
# Check pod status
kubectl get pods -n gracefulshutdown

# View logs
kubectl logs -f <pod-name> -n gracefulshutdown

# Test graceful shutdown
kubectl delete pod <pod-name> -n gracefulshutdown
```

![Gracefulshutdown](image.png)

![Gracefulldown](image-1.png)

### 6. Access Log Files

```bash
# View log files in persistent volume
kubectl exec -it <pod-name> -n gracefulshutdown -- ls /app/logs
kubectl exec -it <pod-name> -n gracefulshutdown -- cat /app/logs/worker-*.log
```

### 7. Cleanup

```bash
# Delete all resources
kubectl delete namespace gracefulshutdown
kubectl delete pv local-logs-pv
```

## Project Structure

```
├── Program.cs              # Application entry point with Serilog configuration
├── QueueWorker.cs          # Background service with graceful shutdown logic
├── Dockerfile              # Multi-stage Docker build
├── K8s/                    # Kubernetes manifests
│   ├── namespace.yaml      # Namespace definition
│   ├── secret.yaml         # Azure Storage connection secret
│   ├── deployment.yaml     # Main deployment with graceful shutdown config
│   ├── pv-docker-desktop.yaml  # Persistent volume for Docker Desktop
│   └── pvc-docker-desktop.yaml # Persistent volume claim
└── README.md              # This file
```

## Key Configuration

- **Termination Grace Period**: 30 seconds (configurable in deployment.yaml)
- **PreStop Hook**: 5-second delay to ensure load balancer updates
- **Log Retention**: Daily rolling logs in `/app/logs/`
- **Queue Polling**: 10-second visibility timeout with 2-second intervals

## Troubleshooting

### Common Issues

1. **Pod stuck in Terminating**: Check if graceful shutdown is taking too long
2. **Logs not persisting**: Verify PV/PVC configuration and mount paths
3. **Connection errors**: Ensure Azure Storage connection string is correct

### Debug Commands

```bash
# Check pod events
kubectl describe pod <pod-name> -n gracefulshutdown

# Check persistent volume
kubectl get pv,pvc -n gracefulshutdown

# View all resources
kubectl get all -n gracefulshutdown
```

## Advanced Topics

### 1. Production Deployment

- **Resource Limits**: Add CPU/memory limits in deployment.yaml
- **Health Checks**: Implement readiness and liveness probes
- **Monitoring**: Add Prometheus metrics and Grafana dashboards
- **Secrets Management**: Use Azure Key Vault with CSI driver

### 2. Scaling and Performance

- **Horizontal Pod Autoscaler**: Scale based on queue length metrics
- **Vertical Pod Autoscaler**: Optimize resource allocation
- **Message Batching**: Process multiple messages per iteration
- **Dead Letter Queues**: Handle poison messages

### 3. Advanced Kubernetes Features

- **Helm Charts**: Use the included Helm chart in `K8s/helm/`
- **Service Mesh**: Integrate with Istio for advanced traffic management
- **GitOps**: Deploy using ArgoCD or Flux
- **Multi-Environment**: Separate dev/staging/prod configurations

### 4. Observability Enhancements

- **Distributed Tracing**: Add OpenTelemetry for request tracing
- **Structured Logging**: Enhance Serilog with correlation IDs
- **Custom Metrics**: Export queue processing metrics to Prometheus
- **Alerting**: Set up alerts for failed message processing

### 5. Security Hardening

- **Pod Security Standards**: Implement restricted security contexts
- **Network Policies**: Restrict pod-to-pod communication
- **Image Scanning**: Integrate vulnerability scanning in CI/CD
- **RBAC**: Implement least-privilege access controls

### 6. CI/CD Integration

```yaml
# Example GitHub Actions workflow
name: Build and Deploy
on:
  push:
    branches: [main]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Build and push Docker image
        run: |
          docker build -t ${{ secrets.DOCKER_HUB_USERNAME }}/graceful-queue-worker:${{ github.sha }} .
          docker push ${{ secrets.DOCKER_HUB_USERNAME }}/graceful-queue-worker:${{ github.sha }}
```

### 7. Alternative Queue Providers

- **Azure Service Bus**: Replace Azure Storage Queue with Service Bus
- **RabbitMQ**: Use RabbitMQ for on-premises scenarios
- **Apache Kafka**: For high-throughput streaming scenarios
- **AWS SQS**: Cross-cloud compatibility

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.