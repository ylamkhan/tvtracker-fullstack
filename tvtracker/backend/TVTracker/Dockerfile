# Use the SDK image so we have the compiler
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else
COPY . .

# Expose the port
EXPOSE 5000

# Start the 'watch' command for auto-updates
CMD ["dotnet", "watch", "run", "--urls", "http://0.0.0.0:5000"]