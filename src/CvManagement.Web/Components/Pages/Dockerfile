FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/CvManagement.Domain/CvManagement.Domain.csproj", "CvManagement.Domain/"]
COPY ["src/CvManagement.Application/CvManagement.Application.csproj", "CvManagement.Application/"]
COPY ["src/CvManagement.Infrastructure/CvManagement.Infrastructure.csproj", "CvManagement.Infrastructure/"]
COPY ["src/CvManagement.Web/CvManagement.Web.csproj", "CvManagement.Web/"]
RUN dotnet restore "CvManagement.Web/CvManagement.Web.csproj"

COPY src/ .

WORKDIR "/src/CvManagement.Web"
RUN dotnet build "CvManagement.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CvManagement.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "CvManagement.Web.dll"]