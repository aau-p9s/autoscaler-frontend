FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Install Node.js
RUN curl -fsSL https://deb.nodesource.com/setup_16.x | bash - \
    && apt-get install -y \
        nodejs \
    && rm -rf /var/lib/apt/lists/*

    
# Copy everything
COPY ./autoscaler-frontend .
# Restore as distinct layers
RUN dotnet restore -a arm64
# Build and publish a release
RUN dotnet publish autoscaler-frontend -c Release -o out


# Build runtime image
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /App
COPY --from=build-env /App/out .
COPY ./autoscaler/autoscaler.py .
RUN ls

ENV PYTHON_VERSION=3.11.6
RUN wget https://www.python.org/ftp/python/$PYTHON_VERSION/Python-$PYTHON_VERSION-linux-aarch64.tar.xz && \
    tar -xf Python-$PYTHON_VERSION-linux-aarch64.tar.xz && \
    mv Python-$PYTHON_VERSION /usr/local/python && \
    ln -s /usr/local/python/bin/python3 /usr/bin/python3 && \
    ln -s /usr/local/python/bin/pip3 /usr/bin/pip3 && \
    rm Python-$PYTHON_VERSION-linux-aarch64.tar.xz


ENTRYPOINT ["dotnet", "autoscaler-frontend.dll", "--scaler", "./autoscaler.py"]
