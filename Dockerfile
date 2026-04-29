# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Driver_Management/Driver_Management.csproj", "Driver_Management/"]
COPY ["Clean.Application/Clean.Application.csproj", "Clean.Application/"]
COPY ["Clean.Domain/Clean.Domain.csproj", "Clean.Domain/"]
COPY ["Clean.Infrastructure/Clean.Infrastructure.csproj", "Clean.Infrastructure/"]
RUN dotnet restore "Driver_Management/Driver_Management.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Driver_Management"
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Driver_Management.dll"]