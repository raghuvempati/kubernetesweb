FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build

# copy content and restore as distinct layers
COPY . ./KubernetesWeb/
WORKDIR /KubernetesWeb
RUN dotnet restore
RUN dotnet publish -c Release -o out

# run the container
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS runtime
COPY --from=build /KubernetesWeb/out ./
ENTRYPOINT ["dotnet", "KubernetesWeb.dll"]