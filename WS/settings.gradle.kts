rootProject.name = "TeamsAIBot-WS"

dependencyResolutionManagement {
    val leoRuntimeVersion: String by settings
    val jooqVersion: String by settings
    val suryaDigitalArtifactsUrl: String by settings

    @Suppress("UnstableApiUsage") // This is an `Incubating` feature. Once the feature is promoted to `Public`, remove this annotation.
    repositories {
        maven {
            name = "suryadigitalArtifactory"
            credentials(PasswordCredentials::class)
            url = uri(suryaDigitalArtifactsUrl)
        }
        mavenCentral()
    }
    versionCatalogs {
        create("libs") {
            from("com.suryadigital.leo:version-catalog:$leoRuntimeVersion")
            plugin("jooq-codegen-gradle", "org.jooq.jooq-codegen-gradle").version(jooqVersion)
        }
    }
}

include("application")
include("jooq")
include("auth")
include("teams-meeting")
include("ai")
