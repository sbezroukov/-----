FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["HomeCenter.csproj", "./"]
RUN dotnet restore "HomeCenter.csproj"
COPY . .
RUN dotnet publish "HomeCenter.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY tests ./tests
COPY Schoolbook ./Schoolbook
COPY TEST-IMPORT-FORMAT.md .
# Copy .env file if it exists (for configuration)
COPY .env* ./
VOLUME ["/app/data"]
ENTRYPOINT ["dotnet", "HomeCenter.dll"]

