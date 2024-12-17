# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY ./Autoscaler .
# Restore as distinct layers
RUN dotnet restore

# Install Node.js
RUN apt-get update && apt-get install -y python3 curl && \
    curl -fsSL https://deb.nodesource.com/setup_18.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Build and publish a release
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App

# Install Node.js
RUN apt-get update && apt-get install -y python3 curl && \
    curl -fsSL https://deb.nodesource.com/setup_18.x | bash - && \
    apt-get install -y nodejs && \
    apt-get clean && rm -rf /var/lib/apt/lists/* 

# Install Pip
RUN apt-get update && apt-get install -y python3-pip && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

# Copy application files
COPY --from=build-env /App/out .
COPY ./Autoscaler/predict.py .
COPY ./Autoscaler/train.py .
COPY ./requirements.txt .

# Install Python dependencies
RUN pip3 install -r requirements.txt

ENTRYPOINT ["dotnet", "Autoscaler.dll", "--scaler", "./predict.py", "--re-trainer", "./train.py"]
