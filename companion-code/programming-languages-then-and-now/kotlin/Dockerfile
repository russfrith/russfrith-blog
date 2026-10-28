FROM eclipse-temurin:25-jdk AS build
WORKDIR /src
RUN apt-get update && apt-get install -y --no-install-recommends curl unzip && rm -rf /var/lib/apt/lists/*
ENV GRADLE_VERSION=9.5.0
RUN curl -fsSL https://services.gradle.org/distributions/gradle-${GRADLE_VERSION}-bin.zip -o gradle.zip \
    && unzip -q gradle.zip -d /opt \
    && rm gradle.zip
ENV PATH="/opt/gradle-${GRADLE_VERSION}/bin:${PATH}"
COPY build.gradle.kts settings.gradle.kts ./
COPY src ./src
RUN gradle shadowJar --no-daemon

FROM eclipse-temurin:25-jre
WORKDIR /app
COPY --from=build /src/build/libs/wordfreq.jar ./wordfreq.jar
ENTRYPOINT ["java", "-jar", "/app/wordfreq.jar"]
