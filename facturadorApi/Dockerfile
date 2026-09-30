# ============================================================================
# Build stage: Compile the application
# ============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy project file and restore dependencies
COPY facturadorApi/facturador.net.csproj ./facturador.net.csproj
RUN dotnet restore facturador.net.csproj

# Copy source code and publish
COPY facturadorApi/ ./
RUN rm -rf Tests
RUN dotnet publish facturador.net.csproj -c Release -o out

# ============================================================================
# Runtime stage: Minimal production image
# ============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install curl for health checks (lightweight)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security (optional, but recommended)
RUN useradd -m -u 1000 appuser

# Create directories for runtime data and secrets
RUN mkdir -p /app/logs /app/.runtime /run/secrets/arca
RUN chown -R appuser:appuser /app /run/secrets/arca

# Copy only the published application (no certificates, no source code)
COPY --from=build-env --chown=appuser:appuser /app/out .

# Copy configuration files (only what's needed at runtime)
COPY --chown=appuser:appuser facturadorApi/conf ./conf

# Set user to non-root
USER appuser

# Expose only the application port
EXPOSE 8080

# Health check endpoint - verifies API + DB connectivity
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set entrypoint
ENTRYPOINT ["dotnet", "facturador.net.dll"]
