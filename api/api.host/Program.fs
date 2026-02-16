module Djambi.Api.Host.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Options
open Pomelo.EntityFrameworkCore.MySql.Infrastructure
open Microsoft.OpenApi.Models
open Serilog
open Serilog.Events
open Newtonsoft.Json.Converters
open Swashbuckle.AspNetCore.SwaggerGen

open Djambi.Api.Db.Interfaces
open Djambi.Api.Db.Repositories
open Djambi.Api.Db.Model
open Djambi.Api.Logic.Interfaces
open Djambi.Api.Logic.Managers
open Djambi.Api.Logic.Services
open Djambi.Api.Model.Configuration
open Djambi.Api.Web
open Djambi.Api.Web.Authentication
open Djambi.Api.Web.Controllers
open Djambi.Api.Logic.Bots
open Djambi.Api.Enums

// Bootstrap logger (before DI is available)
Log.Logger <-
    LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger()

[<EntryPoint>]
let main args =
    try
        let builder = WebApplication.CreateBuilder(args)

        // ── Configuration ────────────────────────────────────────────────
        builder.Configuration.AddEnvironmentVariables("DJAMBI_") |> ignore

        // ── Kestrel URL binding ──────────────────────────────────────────
        let apiAddress = builder.Configuration.GetValue<string>("Api:ApiAddress")
        if not (String.IsNullOrEmpty apiAddress) then
            builder.WebHost.UseUrls(apiAddress) |> ignore

        // ── Serilog ──────────────────────────────────────────────────────
        builder.Host.UseSerilog(fun ctx _ loggerConfig ->
            let template = "{Timestamp:yyyy/MM/dd-HH:mm:ss.fff} {Level:u3} [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
            let levelConfig = ctx.Configuration.GetSection("Log:Levels")

            let mutable cfg =
                loggerConfig
                    .Enrich.FromLogContext()
                    .MinimumLevel.Override("Microsoft", levelConfig.GetValue<LogEventLevel>("Microsoft"))
                    .MinimumLevel.Override("Microsoft.AspNetCore", levelConfig.GetValue<LogEventLevel>("AspNetCore"))
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", levelConfig.GetValue<LogEventLevel>("EfCore"))
                    .WriteTo.Console(outputTemplate = template)

            let dir = ctx.Configuration.GetValue<string>("Log:Directory")
            if not <| String.IsNullOrEmpty dir then
                let logPath = Path.Combine(dir, "server.log")
                cfg <- cfg.WriteTo.File(
                    path = logPath,
                    outputTemplate = template,
                    rollingInterval = RollingInterval.Day,
                    retainedFileCountLimit = Nullable(14))

            cfg |> ignore
        ) |> ignore

        // ── CORS ─────────────────────────────────────────────────────────
        builder.Services.AddCors(fun opt ->
            let originsStr = builder.Configuration.GetValue<string>("Api:AllowedOrigins")
            let allowedOrigins =
                if System.String.IsNullOrEmpty originsStr then [| "http://localhost:3000" |]
                else originsStr.Split(',')
            opt.AddPolicy("ApiCorsPolicy", fun policy ->
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    |> ignore
            )
        ) |> ignore

        // ── Controllers + JSON ───────────────────────────────────────────
        builder.Services
            .AddControllers()
            .AddNewtonsoftJson(fun options ->
                options.SerializerSettings.Converters.Add(StringEnumConverter())
            ) |> ignore

        // ── Health checks ────────────────────────────────────────────────
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<DjambiDbContext>("database") |> ignore

        // ── Configuration binding ────────────────────────────────────────
        builder.Services.Configure<AppSettings>(builder.Configuration) |> ignore
        builder.Services.Configure<SqlSettings>(builder.Configuration.GetSection("Sql")) |> ignore
        builder.Services.Configure<WebServerSettings>(builder.Configuration.GetSection("WebServer")) |> ignore
        builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("Api")) |> ignore

        // ── Serilog (DI) ─────────────────────────────────────────────────
        builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger) |> ignore

        // ── Swagger ──────────────────────────────────────────────────────
        builder.Services.AddSwaggerGen(fun (opt : SwaggerGenOptions) ->
            let info = OpenApiInfo()
            info.Title <- "Djambi-N API"
            info.Description <- "API for Djambi-N"
            info.Version <- "v1"
            opt.SwaggerDoc("v1", info)

            let assemblies = [
                typeof<UserController>.Assembly
                typeof<PlayerKind>.Assembly
            ]
            for a in assemblies do
                let file = a.GetName().Name + ".xml"
                let path = Path.Combine(AppContext.BaseDirectory, file)
                if File.Exists(path) then
                    opt.IncludeXmlComments(path)
        ) |> ignore

        // ── Entity Framework ─────────────────────────────────────────────
        builder.Services.AddDbContext<DjambiDbContext>(fun opt ->
            let cnStr = builder.Configuration.GetValue<string>("Sql:ConnectionString")
            let serverVersion = MySqlServerVersion(Version(8, 0, 0))
            opt.UseMySql(cnStr, serverVersion) |> ignore
        ) |> ignore

        // ── Database layer (Scoped) ──────────────────────────────────────
        builder.Services.AddScoped<IEventRepository, EventRepository>() |> ignore
        builder.Services.AddScoped<IGameRepository, GameRepository>() |> ignore
        builder.Services.AddScoped<IPlayerRepository, PlayerRepository>() |> ignore
        builder.Services.AddScoped<ISearchRepository, SearchRepository>() |> ignore
        builder.Services.AddScoped<IMagicLinkRepository, MagicLinkRepository>() |> ignore
        builder.Services.AddScoped<ISessionRepository, SessionRepository>() |> ignore
        builder.Services.AddScoped<ISnapshotRepository, SnapshotRepository>() |> ignore
        builder.Services.AddScoped<IUserRepository, UserRepository>() |> ignore

        // ── Logic layer (Scoped) ─────────────────────────────────────────
        builder.Services.AddScoped<IEmailService, ConsoleEmailService>() |> ignore
        builder.Services.AddScoped<IEncryptionService, EncryptionService>() |> ignore
        builder.Services.AddScoped<EventService>() |> ignore
        builder.Services.AddScoped<GameCrudService>() |> ignore
        builder.Services.AddScoped<GameStartService>() |> ignore
        builder.Services.AddScoped<IndirectEffectsService>() |> ignore
        builder.Services.AddSingleton<INotificationService, NotificationService>() |> ignore
        builder.Services.AddScoped<PlayerService>() |> ignore
        builder.Services.AddScoped<PlayerStatusChangeService>() |> ignore
        builder.Services.AddScoped<ISessionService, SessionService>() |> ignore
        builder.Services.AddScoped<SelectionOptionsService>() |> ignore
        builder.Services.AddScoped<SelectionService>() |> ignore
        builder.Services.AddScoped<TurnService>() |> ignore

        builder.Services.AddScoped<IBoardManager, BoardManager>() |> ignore
        builder.Services.AddScoped<ISearchManager, SearchManager>() |> ignore
        builder.Services.AddScoped<ISessionManager, SessionManager>() |> ignore
        builder.Services.AddScoped<ISnapshotManager, SnapshotManager>() |> ignore
        builder.Services.AddScoped<IUserManager, UserManager>() |> ignore
        builder.Services.AddScoped<IEventManager, EventManager>() |> ignore
        builder.Services.AddScoped<IGameManager, GameManager>() |> ignore
        builder.Services.AddScoped<IPlayerManager, PlayerManager>() |> ignore
        builder.Services.AddScoped<ITurnManager, TurnManager>() |> ignore

        // ── Bot players (Singleton) ────────────────────────────────────────
        builder.Services.AddSingleton<IBotRegistry>(fun _ ->
            let registry = BotRegistry()
            let iRegistry = registry :> IBotRegistry
            iRegistry.register(RandomBot())
            iRegistry.register(MinimaxBot(2))
            iRegistry
        ) |> ignore

        builder.Services.AddHostedService<Djambi.Api.Host.BotRunner>() |> ignore

        // ── Web layer (Scoped) ───────────────────────────────────────────
        builder.Services.AddScoped<CookieProvider>() |> ignore

        // ── Authentication ───────────────────────────────────────────────
        builder.Services.AddDjambiAuthentication() |> ignore

        // ── Build ────────────────────────────────────────────────────────
        let app = builder.Build()

        // ── Validate required configuration ─────────────────────────────
        let sqlConfig =
            app.Services.GetRequiredService<IOptions<SqlSettings>>().Value
        if String.IsNullOrEmpty sqlConfig.connectionString then
            failwith "Sql:ConnectionString is not configured. Set DJAMBI_Sql__ConnectionString environment variable."

        // ── Apply database migrations ──────────────────────────────────────
        use scope = app.Services.CreateScope()
        let dbContext = scope.ServiceProvider.GetRequiredService<DjambiDbContext>()
        Log.Logger.Information("Applying database migrations...")
        dbContext.Database.Migrate() |> ignore

        let config =
            app.Services.GetService(typeof<IOptions<AppSettings>>) :?> IOptions<AppSettings>
            |> fun x -> x.Value
        Log.Logger.Information("Configuration loaded (secrets redacted)")

        // ── Middleware pipeline ───────────────────────────────────────────

        // Correlation ID: propagate from request header or generate new
        app.Use(fun ctx (next : RequestDelegate) ->
            task {
                let header = ctx.Request.Headers.["X-Correlation-Id"]
                let correlationId =
                    if header.Count > 0 && not (String.IsNullOrEmpty(header.[0]))
                    then header.[0]
                    else Guid.NewGuid().ToString("N")
                ctx.Items.["CorrelationId"] <- correlationId
                ctx.Response.Headers.["X-Correlation-Id"] <- correlationId
                use _ = Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId)
                do! next.Invoke(ctx)
            } :> System.Threading.Tasks.Task
        ) |> ignore

        app.UseSerilogRequestLogging() |> ignore
        app.UseMiddleware<ErrorHandlingMiddleware>() |> ignore
        app.UseRouting() |> ignore
        app.UseCors("ApiCorsPolicy") |> ignore
        app.UseAuthentication() |> ignore
        app.UseAuthorization() |> ignore
        app.UseWebSockets() |> ignore

        app.MapControllers() |> ignore
        app.MapHealthChecks("/status") |> ignore

        // ── Swagger (Development only) ──────────────────────────────────
        if app.Environment.IsDevelopment() then
            app.UseSwagger() |> ignore
            app.UseSwaggerUI(fun opt ->
                opt.SwaggerEndpoint("/swagger/v1/swagger.json", "Djambi API V1")
            ) |> ignore

        // ── Run ──────────────────────────────────────────────────────────
        Log.Logger.Information("Starting host.")
        app.Run()
        0
    with
    | ex ->
        Log.Logger.Fatal(ex, "Application terminated unexpectedly.")
        -1
    // Serilog flushes on process exit via UseSerilog()
