# Runs both benchmark suites and writes BenchmarkDotNet's reports to /out.
#
# Build:
#   docker build -t span-benchmarks .
#
# Run (mount a host folder to collect the results):
#   docker run --rm -v "$PWD/results:/out" span-benchmarks
#
# BenchmarkDotNet benchmarks run best on real hardware without contention
# from other containers; expect the raw numbers to vary run-to-run, but the
# 0 B allocation results and ~1.00 timing ratios should hold.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY SpanBenchmarks.CSharp14/ SpanBenchmarks.CSharp14/
COPY SpanBenchmarks.CSharp13/ SpanBenchmarks.CSharp13/

RUN dotnet build SpanBenchmarks.CSharp14/SpanBenchmarks.CSharp14.csproj -c Release
RUN dotnet build SpanBenchmarks.CSharp13/SpanBenchmarks.CSharp13.csproj -c Release

FROM build AS runtime
WORKDIR /src
RUN mkdir -p /out

CMD ["sh", "-c", "\
  cd /src/SpanBenchmarks.CSharp14 && \
  dotnet run -c Release --no-build -- --filter '*' && \
  cd /src/SpanBenchmarks.CSharp13 && \
  dotnet run -c Release --no-build -- --filter '*' && \
  cp -r /src/SpanBenchmarks.CSharp14/BenchmarkDotNet.Artifacts /out/CSharp14.Artifacts && \
  cp -r /src/SpanBenchmarks.CSharp13/BenchmarkDotNet.Artifacts /out/CSharp13.Artifacts"]
