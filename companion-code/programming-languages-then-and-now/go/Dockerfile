FROM golang:1.26.4-bookworm AS build
WORKDIR /src
COPY go.mod wordfreq.go ./
RUN CGO_ENABLED=0 go build -o /app/wordfreq ./wordfreq.go

FROM gcr.io/distroless/static-debian12
COPY --from=build /app/wordfreq /app/wordfreq
ENTRYPOINT ["/app/wordfreq"]
