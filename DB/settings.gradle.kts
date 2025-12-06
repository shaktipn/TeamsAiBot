rootProject.name = "DB"

dependencyResolutionManagement {
    val leoRuntimeVersion: String by settings
    @Suppress("UnstableApiUsage") // This is an `Incubating` feature. Once the feature is promoted to `Public`, remove this annotation.
    repositories {
        maven {
            name = "suryadigitalArtifactory"
            credentials(PasswordCredentials::class)
            url = uri("https://artifacts.surya-digital.in/repository/maven-releases/")
        }
        mavenCentral()
    }
    versionCatalogs {
        create("libs") {
            from("com.suryadigital.leo:version-catalog:$leoRuntimeVersion")
        }
    }
}
