# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY FileProcessingApp.csproj .
RUN dotnet restore --runtime linux-x64

# Copy source code and build
COPY . .
RUN dotnet publish FileProcessingApp.csproj -c Release -o /app/publish \
    --runtime linux-x64 \
    --self-contained false \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS final
WORKDIR /app

# Create non-root user
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup

# Copy published app
COPY --from=build /app/publish .

# Set ownership and switch to non-root user
RUN chown -R appuser:appgroup /app
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD pgrep -f "dotnet FileProcessingApp.dll" > /dev/null || exit 1

ENTRYPOINT ["dotnet", "FileProcessingApp.dll"]