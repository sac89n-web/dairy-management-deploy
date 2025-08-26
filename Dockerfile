# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Reports/Reports.csproj src/Reports/
COPY src/Web/Dairy.Web.csproj src/Web/

# Restore dependencies
RUN dotnet restore src/Web/Dairy.Web.csproj

# Copy source code
COPY src/ src/

# Build the application
WORKDIR /src/src/Web
RUN dotnet build Dairy.Web.csproj -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish Dairy.Web.csproj -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "Dairy.Web.dll"]