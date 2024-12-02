# Use an official .NET 7 SDK base image for ARM64
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base

# Update and install prerequisites
RUN apt-get update && apt-get install -y \
    curl \
    gnupg \
    software-properties-common \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Install Node.js 16.x
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash - \
    && apt-get update && apt-get install -y nodejs \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Install Python3
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Verify installations
#RUN dotnet --version && node --version && python3 --version

# Set the working directory
WORKDIR /app

COPY . /app


CMD ["dotnet", "run", "--project", "autoscaler-frontend/autoscaler-frontend"]
#["dotnet", "autoscaler-frontend.dll", "--scaler", "./autoscaler.py"]
