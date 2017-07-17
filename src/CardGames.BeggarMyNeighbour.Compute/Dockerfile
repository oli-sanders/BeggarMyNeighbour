FROM microsoft/dotnet:2.0.0-preview2-sdk AS build-env
WORKDIR /BeggarCompute

# copy everything else and build
COPY . .

RUN dotnet restore

RUN dotnet publish -c Release -o out -r debian.8-x64

# build runtime image
FROM microsoft/dotnet:2.0.0-preview2-runtime-deps
WORKDIR /app
COPY --from=build-env /app/out ./
ENTRYPOINT ["./BeggarCompute"]