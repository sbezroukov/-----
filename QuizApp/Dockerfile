FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["QuizApp.csproj", "./"]
RUN dotnet restore "QuizApp.csproj"
COPY . .
RUN dotnet publish "QuizApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY tests ./tests
VOLUME ["/app/data"]
ENTRYPOINT ["dotnet", "QuizApp.dll"]

