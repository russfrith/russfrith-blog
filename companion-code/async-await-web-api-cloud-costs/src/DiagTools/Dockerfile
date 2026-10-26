FROM mcr.microsoft.com/dotnet/sdk:10.0
RUN dotnet tool install --global dotnet-counters \
    && dotnet tool install --global dotnet-trace
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet-counters --version > /tool-versions.txt 2>&1 || true
WORKDIR /results
COPY src/DiagTools/collect.sh /collect.sh
ENTRYPOINT ["/bin/bash", "/collect.sh"]
