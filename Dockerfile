# STAGE01 - Build application and its dependencies
FROM mcr.microsoft.com/dotnet/core/sdk:3.0-alpine AS build
WORKDIR /app

COPY . ./
RUN dotnet restore

# STAGE02 - Publish the application
FROM build AS publish
WORKDIR /app/Net.Bluewalk.VMware.AutoShutdown
RUN dotnet publish -c Release -o ../out
RUN rm ../out/*.pdb

# STAGE03 - Create the final image
FROM mcr.microsoft.com/dotnet/core/runtime:3.0-alpine AS runtime
LABEL Description="VMware AutoShutdown image" \
      Maintainer="Bluewalk"

WORKDIR /app
COPY --from=publish /app/out ./
COPY shutdown.sh ./

#ENTRYPOINT ["dotnet", "Net.Bluewalk.VMware.AutoShutdown.dll"]
CMD ["dotnet", "Net.Bluewalk.VMware.AutoShutdown.dll"]