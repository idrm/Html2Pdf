FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build
COPY Html2Pdf.csproj Directory.Packages.props ./
RUN dotnet restore Html2Pdf.csproj
COPY . .
RUN dotnet publish Html2Pdf.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
EXPOSE 8080
WORKDIR /app
COPY --from=build /app .

# The commit this image was built from, reported on /buildz for the deploy workflow to poll.
ARG BUILD_SHA=unknown
ENV BUILD_SHA=$BUILD_SHA

USER $APP_UID
ENTRYPOINT ["dotnet", "Html2Pdf.dll"]
