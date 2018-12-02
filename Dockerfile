FROM microsoft/dotnet:2.0-sdk as builder
ENV DOTNET_CLI_TELEMETRY_OUTPUT=1

RUN mkdir -p /root/src/app
WORKDIR /root/src/app
COPY CryptoCompareAPI CryptoCompareAPI
COPY CryptoMarketData CryptoMarketData

RUN dotnet restore ./CryptoMarketData/CryptoMarketData.csproj
#RUN dotnet publish -c release -o ./published -r linux-arm ./CryptoMarketData/CryptoMarketData.csproj
RUN dotnet publish -c release -o ./published ./CryptoMarketData/CryptoMarketData.csproj

#FROM microsoft/dotnet:2.0.0-runtime-stretch-arm32v7
FROM microsoft/dotnet:2.0.0-runtime

WORKDIR /root/
COPY --from=builder /root/src/app/CryptoMarketData/published .

CMD ["dotnet", "./CryptoMarketData.dll"]