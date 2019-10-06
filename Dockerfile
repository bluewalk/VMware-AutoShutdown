# STAGE01 - Build application and its dependencies
FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
WORKDIR /app

COPY . ./
RUN dotnet restore

# STAGE02 - Publish the application
FROM build AS publish
WORKDIR /app/Net.Bluewalk.VMware.AutoShutdown
RUN dotnet publish -c Release -o ../out
RUN rm ../out/*.pdb

# STAGE03 - Create the final image
FROM bluewalk/vmware-powercli-dotnet-runtime AS runtime
WORKDIR /app
COPY --from=publish /app/out ./
COPY shutdown.ps1 ./

#ENTRYPOINT ["dotnet", "Net.Bluewalk.VMware.AutoShutdown.dll"]
CMD ["dotnet", "Net.Bluewalk.VMware.AutoShutdown.dll"]