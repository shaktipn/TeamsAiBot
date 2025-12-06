import org.flywaydb.gradle.task.AbstractFlywayTask
import java.io.File
import java.nio.file.Path
import kotlin.io.path.listDirectoryEntries
import kotlin.io.path.name

buildscript {
    val leoRuntimeVersion: String by project
    repositories {
        maven {
            name = "suryadigitalArtifactory"
            credentials(PasswordCredentials::class)
            url = uri("https://artifacts.surya-digital.in/repository/maven-releases/")
        }
    }
    dependencies {
        classpath(libs.postgresql)
        classpath("com.suryadigital.leo:plugins:$leoRuntimeVersion")
        classpath(libs.flyway.database.postgresql)
    }
}

plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.flyway)
}

dependencies {
    implementation(libs.postgresql)
}

tasks.withType<AbstractFlywayTask> {
    val dbHostName = "localhost"
    val dbPortNumber = System.getenv("POSTGRES_PORT")?.toInt() ?: 543255
    val dbName = System.getenv("POSTGRES_DB") ?: "teamsaibot"
    val dbUser = System.getenv("POSTGRES_USER") ?: "teamsaibot"
    val dbPassword = System.getenv("POSTGRES_PASSWORD") ?: "Teamsaibot@1234"
    val env = System.getenv("ENV") ?: "dev"

    url = "jdbc:postgresql://$dbHostName:$dbPortNumber/$dbName"
    user = dbUser
    password = dbPassword
    locations = arrayOf("filesystem:$rootDir/postgresql/migrations/common", "filesystem:$rootDir/postgresql/migrations/$env")
    validateOnMigrate = true
    validateMigrationNaming = true
    cleanDisabled = true
}
