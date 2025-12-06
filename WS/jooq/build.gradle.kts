plugins {
    alias(libs.plugins.kotlin.jvm)
    alias(libs.plugins.jooq.codegen.gradle)
}

buildscript {
    dependencies {
        classpath(libs.leo.jooq)
        classpath(libs.jooq.codegen)
        classpath(libs.postgresql)
    }
}

sourceSets {
    main {
        kotlin {
            srcDirs("src")
        }
    }
}

dependencies {
    implementation(libs.jooq)
    runtimeOnly(libs.postgresql)
}

jooq {
    val dbHostName = System.getenv("DB_HOST") ?: "localhost"
    val dbPortNumber = System.getenv("DB_PORT") ?: "54355"
    val dbName = System.getenv("DB_NAME") ?: "teamsaibot"
    val dbUser = System.getenv("DB_USER") ?: "teamsaibot"
    val dbPassword = System.getenv("DB_PASSWORD") ?: "Teamsaibot@1234"
    configuration {
        jdbc {
            driver = "org.postgresql.Driver"
            url = "jdbc:postgresql://$dbHostName:$dbPortNumber/$dbName"
            user = dbUser
            password = dbPassword
        }
        generator {
            name = "org.jooq.codegen.KotlinGenerator"
            generate {
                isImplicitJoinPathsToMany = false
            }
            database {
                inputSchema = "public"
                forcedTypes {
                    forcedType {
                        name = "INSTANT"
                        includeTypes = "TIMESTAMPTZ|TIMESTAMP WITH TIME ZONE"
                    }
                }
            }
            strategy {
                name = "com.suryadigital.leo.jooq.CamelCaseNameGeneratorStrategy"
            }
            target {
                packageName = "com.suryadigital.teamsaibot.jooq"
                directory = "$projectDir/src"
                withClean(true)
            }
        }
    }
}

kotlin {
    val javaVersion: String by project
    jvmToolchain(javaVersion.toInt())
}

tasks {
    compileKotlin {
        dependsOn("jooqCodegen")
    }
    jar {
        enabled = true
    }
}
