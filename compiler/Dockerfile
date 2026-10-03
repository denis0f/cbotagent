FROM mcr.microsoft.com/dotnet/sdk:6.0

WORKDIR /src

COPY src.csproj .

RUN dotnet restore

COPY . .

ENTRYPOINT ["dotnet", "build", "src.csproj", "-c", "Release"]