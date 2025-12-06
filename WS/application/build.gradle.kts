plugins {
    application
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.spotless)
    alias(libs.plugins.ktor)
    alias(libs.plugins.kotlinx.serialization)
}

application {
    mainClass.set("com.suryadigital.teamsaibot.application.ApplicationKt")
}

sourceSets {
    main {
        kotlin {
            srcDirs("src")
        }
        resources {
            srcDirs("resources")
        }
    }
}

dependencies {
    implementation("com.suryadigital.teamsaibot:auth-service-boilerplate:SNAPSHOT")
    implementation("com.suryadigital.teamsaibot:teams-meeting-service-boilerplate:SNAPSHOT")
    implementation(project(":auth"))
    implementation(project(":teams-meeting"))
    implementation(project(":ai"))

    implementation("io.ktor:ktor-server-websockets:3.1.3")


    implementation(libs.postgresql)
    implementation(libs.koin.ktor)
    implementation(libs.leo.ktor)
    implementation(libs.leo.basedb)
    implementation(libs.leo.kotlinx.serialization.json)
    implementation(libs.leo.crypto)
    implementation(libs.ktor.server.core.jvm)
    implementation(libs.ktor.server.metrics.jvm)
    implementation(libs.ktor.server.netty)
    implementation(libs.logback)
    implementation(libs.kotlinx.serialization)
    implementation(libs.leo.inline.logger)
}

tasks {
    startScripts {
        enabled = false
    }
    startShadowScripts {
        enabled = false
    }
    shadowJar {
        enabled = true
        archiveFileName.set("application.jar")
    }
}

spotless {
    kotlin {
        ktlint()
            .setEditorConfigPath("$rootDir/.editorconfig")
    }
}

kotlin {
    val javaVersion: String by project
    jvmToolchain(javaVersion.toInt())
}
