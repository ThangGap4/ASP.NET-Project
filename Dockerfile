# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
COPY QuizAI.Api/QuizAI.Api.csproj QuizAI.Api/

# Restore dependencies
RUN dotnet restore QuizAI.Api/QuizAI.Api.csproj

# Copy the rest of the source
COPY QuizAI.Api/ QuizAI.Api/

# Build and publish
WORKDIR /src/QuizAI.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create uploads directory
RUN mkdir -p /app/uploads && chmod 755 /app/uploads

# Copy published output
COPY --from=build /app/publish .

# Expose port (Render uses PORT env var, default 8080)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "QuizAI.Api.dll"]
