FROM mcr.microsoft.com/dotnet/core/sdk:2.1 AS sdk
COPY . .
RUN dotnet publish /sandbox/MultiContainedApp/MultiContainedApp.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/runtime:2.1
COPY --from=sdk /app .
ENTRYPOINT ["dotnet", "MultiContainedApp.dll"]