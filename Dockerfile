FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

COPY ./autoscaler-frontend/autoscaler-frontend .

COPY ./autoscaler/autoscaler.py .

EXPOSE 44411

# Install Python
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    apt-get clean && rm -rf /var/lib/apt/lists/*

CMD ["dotnet", "run", "--scaler", "./autoscaler.py"]