# Stage 1: build & publish
# Uses the full SDK image (has the compiler, NuGet, everything needed to
# build) — this stage's size never ships in the final image.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy just the .csproj first and restore separately from the rest of the
# source. Docker caches each instruction — as long as the .csproj doesn't
# change, this restore layer is reused on rebuilds instead of re-downloading
# every NuGet package every time you change a .cs file.
COPY ["ECommerceAPI.csproj", "./"]
RUN dotnet restore "ECommerceAPI.csproj"

# Now copy everything else and publish.
COPY . .
RUN dotnet publish "ECommerceAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: runtime
# Much smaller image — just the ASP.NET runtime, no SDK/compiler at all.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# .NET 8's container images listen on 8080 by default
EXPOSE 8080
ENTRYPOINT ["dotnet", "ECommerceAPI.dll"]
