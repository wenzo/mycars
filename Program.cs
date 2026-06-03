using Dapper;
using MyCars.Configuration;
using MyCars.Infrastructure.Database;
using MyCars.Infrastructure.Push;
using MyCars.Infrastructure.Http;
using MyCars.Domain.Repositories;
using MyCars.Repositories.Rest;
using MyCars.Repositories.Postgres;

// ── CLI utilities (non avviano il server) ─────────────────────────────────────
if (args is ["hash-password", var pwd])
{
    Console.WriteLine(BCrypt.Net.BCrypt.HashPassword(pwd, workFactor: 12));
    return;
}
if (args is ["generate-vapid-keys"])
{
    var (pub, priv) = VapidHelper.GenerateKeys();
    Console.WriteLine($"dotnet user-secrets set \"Vapid:PublicKey\"  \"{pub}\"");
    Console.WriteLine($"dotnet user-secrets set \"Vapid:PrivateKey\" \"{priv}\"");
    return;
}

// Dapper: mappa automaticamente snake_case → PascalCase (operator_id → OperatorId)
DefaultTypeMap.MatchNamesWithUnderscores = true;

// ─────────────────────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// ── Opzioni di configurazione ─────────────────────────────────────────────────
builder.Services.Configure<DatabaseOptions>   (builder.Configuration.GetSection("Database"));
builder.Services.Configure<SupabaseOptions>   (builder.Configuration.GetSection("Supabase"));
builder.Services.Configure<AdminOptions>      (builder.Configuration.GetSection("Admin"));
builder.Services.Configure<SuperAdminOptions> (builder.Configuration.GetSection("SuperAdmin"));
builder.Services.Configure<JwtOptions>        (builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<VapidOptions>      (builder.Configuration.GetSection("Vapid"));
builder.Services.Configure<SmtpOptions>       (builder.Configuration.GetSection("Smtp"));

// ── Cookie authentication (pannello admin) ────────────────────────────────────
var adminCfg = builder.Configuration.GetSection("Admin");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name      = adminCfg["CookieName"] ?? "mycars_admin";
        options.Cookie.HttpOnly  = true;
        options.Cookie.SameSite  = SameSiteMode.Strict;
        options.ExpireTimeSpan   = TimeSpan.FromHours(adminCfg.GetValue<int>("SessionHours", 8));
        options.SlidingExpiration = true;

        // Per le chiamate API restituisce 401 invece di redirect 302
        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            else
                ctx.Response.Redirect("/admin/login.html");
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// ── Controller + JSON ─────────────────────────────────────────────────────────
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy        = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition      = JsonIgnoreCondition.WhenWritingNull;
    });

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(o =>
        o.AddDefaultPolicy(p =>
            p.WithOrigins(allowedOrigins)
             .AllowAnyHeader()
             .AllowAnyMethod()
             .AllowCredentials()));
}

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Database (dual-mode) ──────────────────────────────────────────────────────
var dbProvider = builder.Configuration["Database:Provider"] ?? "Rest";

if (dbProvider.Equals("Rest", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<ISupabaseRestClient, SupabaseRestClient>();
    builder.Services.AddScoped<IOperatorRepository,             SupabaseRestOperatorRepository>();
    builder.Services.AddScoped<IOperatorUserRepository,         SupabaseRestOperatorUserRepository>();
    builder.Services.AddScoped<IOperatorRegistrationRepository, SupabaseRestOperatorRegistrationRepository>();
    builder.Services.AddScoped<IVehicleRepository,              SupabaseRestVehicleRepository>();
    builder.Services.AddScoped<INewsRepository,                 SupabaseRestNewsRepository>();
    builder.Services.AddScoped<ILeadRepository,                 SupabaseRestLeadRepository>();
    builder.Services.AddScoped<IPushRepository,                 SupabaseRestPushRepository>();
    builder.Services.AddScoped<IBranchRepository,               SupabaseRestBranchRepository>();
    builder.Services.AddScoped<IDepartmentRepository,           SupabaseRestDepartmentRepository>();
    builder.Services.AddScoped<IScheduledPushRepository,        SupabaseRestScheduledPushRepository>();
}
else if (dbProvider.Equals("Npgsql", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IDbConnectionFactory, PostgresConnectionFactory>();
    builder.Services.AddScoped<IOperatorRepository,             PostgresOperatorRepository>();
    builder.Services.AddScoped<IOperatorUserRepository,         PostgresOperatorUserRepository>();
    builder.Services.AddScoped<IOperatorRegistrationRepository, PostgresOperatorRegistrationRepository>();
    builder.Services.AddScoped<IVehicleRepository,              PostgresVehicleRepository>();
    builder.Services.AddScoped<INewsRepository,                 PostgresNewsRepository>();
    builder.Services.AddScoped<ILeadRepository,                 PostgresLeadRepository>();
    builder.Services.AddScoped<IPushRepository,                 PostgresPushRepository>();
    builder.Services.AddScoped<IBranchRepository,               PostgresBranchRepository>();
    builder.Services.AddScoped<IDepartmentRepository,           PostgresDepartmentRepository>();
    builder.Services.AddScoped<IScheduledPushRepository,        PostgresScheduledPushRepository>();
}
else
{
    throw new InvalidOperationException(
        $"Database:Provider non valido: '{dbProvider}'. Valori attesi: Rest | Npgsql");
}

// ── Scheduled push ────────────────────────────────────────────────────────────
builder.Services.AddHostedService<PushSchedulerService>();

// ── File storage ─────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();


// ── Email ─────────────────────────────────────────────────────────────────────
var smtpCfg = builder.Configuration.GetSection("Smtp").Get<SmtpOptions>() ?? new();
if (!string.IsNullOrWhiteSpace(smtpCfg.Host) && !string.IsNullOrWhiteSpace(smtpCfg.FromEmail))
    builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
else
    builder.Services.AddSingleton<IEmailService, NullEmailService>();

// ── VAPID / Web Push ──────────────────────────────────────────────────────────
var vapidCfg = builder.Configuration.GetSection("Vapid").Get<VapidOptions>() ?? new();
if (!string.IsNullOrWhiteSpace(vapidCfg.PublicKey) && !string.IsNullOrWhiteSpace(vapidCfg.PrivateKey))
    builder.Services.AddSingleton<IWebPushService, VapidWebPushService>();
else
    builder.Services.AddSingleton<IWebPushService, NullWebPushService>();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

if (allowedOrigins.Length > 0)
    app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Protegge le aree admin e superadmin (statici)
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path;

    if (path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase)
        && !path.Equals("/admin/login.html", StringComparison.OrdinalIgnoreCase))
    {
        if (ctx.User?.IsInRole("Admin") != true)
        {
            ctx.Response.Redirect("/accesso.html");
            return;
        }
    }

    if (path.StartsWithSegments("/superadmin", StringComparison.OrdinalIgnoreCase)
        && !path.Equals("/superadmin/login.html", StringComparison.OrdinalIgnoreCase))
    {
        if (ctx.User?.IsInRole("SuperAdmin") != true)
        {
            ctx.Response.Redirect("/accesso.html");
            return;
        }
    }

    await next(ctx);
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/accesso.html"));

app.Run();
