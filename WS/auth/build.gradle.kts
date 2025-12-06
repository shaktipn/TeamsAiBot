plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.spotless)
    alias(libs.plugins.kotlinx.serialization)
}

sourceSets {
    main {
        kotlin.srcDirs("src")
    }
}

tasks.jar {
    enabled = true
}

dependencies {
    implementation(project(":jooq"))
    implementation("com.suryadigital.teamsaibot:auth-service-boilerplate:SNAPSHOT")
    implementation("com.suryadigital.teamsaibot:auth-interface:SNAPSHOT")

    implementation(libs.leo.ktor)
    implementation(libs.leo.crypto)
    implementation(libs.leo.inline.logger)
    implementation(libs.leo.basedb)
    implementation(libs.leo.types)
    implementation(libs.typesafe.config)
    implementation(libs.ktor.server.core.jvm)
    implementation(libs.kotlinx.serialization)
    implementation(libs.koin.ktor)
    implementation(libs.leo.kt.utils)
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
