FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Blog.slnx ./
COPY Directory.Build.props ./
COPY src ./src
COPY tests ./tests
COPY content ./content
RUN dotnet restore Blog.slnx
RUN dotnet publish src/Blog.Api/Blog.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Blog.Api.dll"]
