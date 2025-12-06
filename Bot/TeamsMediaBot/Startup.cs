using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Identity.Client;
using TeamsMediaBot.Configuration;
using TeamsMediaBot.Services;

namespace TeamsMediaBot
{
    /// <summary>
    /// Application startup configuration.
    /// Configures services, dependency injection, and middleware pipeline.
    /// </summary>
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Configures application services and dependency injection.
        /// </summary>
        /// <param name="services">Service collection to configure.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("[TRACE] Startup.ConfigureServices() started");
            Console.Out.Flush();

            // Add MVC controllers
            services.AddControllers();

            // Configure strongly-typed configuration classes
            services.Configure<AzureAdConfiguration>(
                Configuration.GetSection(key: "AzureAd"));

            services.Configure<BotConfiguration>(
                Configuration.GetSection(key: "Bot"));

            services.Configure<KtorConfiguration>(
                Configuration.GetSection(key: "Ktor"));

            services.Configure<DeepgramConfiguration>(
                Configuration.GetSection(key: "Deepgram"));

            // Register MSAL client for Graph API authentication
            var azureAdConfig = Configuration.GetSection("AzureAd").Get<AzureAdConfiguration>();
            if (azureAdConfig == null)
            {
                throw new InvalidOperationException("AzureAd configuration is missing.");
            }
            var confidentialClient = ConfidentialClientApplicationBuilder
                .Create(clientId: azureAdConfig.ClientId)
                .WithClientSecret(clientSecret: azureAdConfig.ClientSecret)
                .WithTenantId(tenantId: azureAdConfig.TenantId)
                .Build();
            services.AddSingleton<IConfidentialClientApplication>(confidentialClient);

            // Register Graph logger for debugging
            var graphLogger = new GraphLogger(
                component: "TeamsMediaBot",
                redirectToTrace: true);
            services.AddSingleton<IGraphLogger>(implementationInstance: graphLogger);

            // Register HTTP client for Ktor service
            // Each KtorService instance gets a fresh HttpClient
            services.AddHttpClient<KtorService>();

            // Register HTTP client and service for Graph API operations
            services.AddHttpClient<GraphCallService>();
            services.AddTransient<GraphCallService>();

            // Register core services
            // BotMediaService is a singleton because it maintains the Graph Communications Client
            services.AddSingleton<BotMediaService>();

            // Register transient services for per-session isolation
            // Each meeting session gets fresh instances to prevent cross-contamination
            services.AddTransient<KtorService>();
            services.AddTransient<DeepgramService>();
            // Note: MeetingSessionManager is NOT registered in DI
            // It's created manually by BotMediaService.CreateSessionManager() for each session

            // Add logging
            services.AddLogging(configure: builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });
        }

        /// <summary>
        /// Configures the HTTP request pipeline and middleware.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="env">Web host environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable routing
            app.UseRouting();

            // Configure endpoints
            app.UseEndpoints(configure: endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
