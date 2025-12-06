import org.jetbrains.kotlin.gradle.dsl.JvmTarget
import org.jetbrains.kotlin.gradle.tasks.KotlinJvmCompile

plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.spotless)
}

subprojects {
    buildscript {
        repositories {
            val suryaDigitalArtifactsUrl: String by project
            mavenCentral()
            maven {
                name = "suryadigitalArtifactory"
                credentials(PasswordCredentials::class)
                url = uri(suryaDigitalArtifactsUrl)
            }
        }
    }
    tasks.withType<KotlinJvmCompile> {
        val javaVersion: String by project
        compilerOptions {
            jvmTarget.set(JvmTarget.fromTarget(javaVersion))
        }
    }
    repositories {
        val suryaDigitalArtifactsUrl: String by project
        mavenLocal() // Required for fetching locally published kt-artifacts.
        mavenCentral()
        maven {
            name = "suryadigitalArtifactory"
            credentials(PasswordCredentials::class)
            url = uri(suryaDigitalArtifactsUrl)
        }
    }
    tasks.withType<Tar> {
        duplicatesStrategy = DuplicatesStrategy.EXCLUDE
        enabled = false
    }
    tasks.withType<Zip> {
        duplicatesStrategy = DuplicatesStrategy.EXCLUDE
        enabled = false
    }
}

repositories(RepositoryHandler::mavenCentral)

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
