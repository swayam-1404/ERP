FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY backend ./backend
COPY docs/research/verified_institution_data.json ./docs/research/verified_institution_data.json
RUN dotnet publish backend/src/VHEI.Api -c Release -o /out
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_HTTP_PORTS=8080
RUN mkdir -p /app/keys && chown -R $APP_UID /app/keys
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "VHEI.Api.dll"]
