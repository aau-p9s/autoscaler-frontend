FROM mcr.microsoft.com/dotnet/sdk:7.0 AS base
RUN apt-get update && apt-get install -y python3 python3-pip \
  && ln -sf /usr/bin/python3 /usr/local/bin/python \
  && ln -sf /usr/bin/python3 /usr/bin/python \
  && pip3 install --upgrade pip
WORKDIR /app
EXPOSE 80

# Build image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Install Node.js (if needed)
RUN curl -fsSL https://deb.nodesource.com/setup_14.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src
COPY ["autoscaler-frontend.csproj", "./"]
RUN dotnet restore "autoscaler-frontend.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "autoscaler-frontend.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "autoscaler-frontend.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Add the Python script to the correct location
COPY ./autoscaler/autoscaler.py /app/autoscaler.py

# Set entry point
ENTRYPOINT ["dotnet", "autoscaler-frontend.dll"]