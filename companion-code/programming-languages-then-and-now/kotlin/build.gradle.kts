plugins {
    kotlin("jvm") version "2.4.0"
    application
    id("com.gradleup.shadow") version "8.3.5"
}

repositories {
    mavenCentral()
}

dependencies {
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.10.1")
}

application {
    mainClass.set("WordfreqKt")
}

tasks.shadowJar {
    archiveFileName.set("wordfreq.jar")
    manifest {
        attributes["Main-Class"] = "WordfreqKt"
    }
}
