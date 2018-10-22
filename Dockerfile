FROM microsoft/dotnet:2.1-sdk-alpine as build
WORKDIR /app 
COPY src . 
RUN dotnet publish /app/Consumer/Consumer.csproj -c Release -o /app/out

FROM microsoft/dotnet:2.1-runtime-alpine3.7 as deployment
WORKDIR /app 
COPY --from=build /app/out . 
ENTRYPOINT [ "dotnet", "Consumer.dll" ]
