FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash - \
    && apt-get install -y \
        nodejs \
    && rm -rf /var/lib/apt/lists/*

    
    # Copy everything
    COPY ./autoscaler-frontend .
    # Restore as distinct layers
    RUN dotnet restore
    # Build and publish a release
    RUN dotnet publish autoscaler-frontend -c Release -o out
    
    # Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /App
COPY --from=build-env /App/out .
COPY ./autoscaler/autoscaler.py .
RUN apt-get update && apt-get -y install python3
ENTRYPOINT ["dotnet", "autoscaler-frontend.dll", "--scaler", "./autoscaler.py"]
