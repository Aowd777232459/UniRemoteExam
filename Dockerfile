FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY UniRemoteExam.csproj ./
RUN dotnet restore UniRemoteExam.csproj
COPY . ./
RUN dotnet publish UniRemoteExam.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "UniRemoteExam.dll"]
