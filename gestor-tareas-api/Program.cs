using System.Text;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using GestorTareas.Api.Data;
using GestorTareas.Api.Middleware;
using GestorTareas.Api.Models;
using GestorTareas.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskRino API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Escribe el access token JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>()
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.Key) && builder.Environment.IsDevelopment())
    jwt.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 bytes.");
builder.Services.Configure<JwtOptions>(options =>
{
    options.Issuer = jwt.Issuer;
    options.Audience = jwt.Audience;
    options.Key = jwt.Key;
    options.AccessTokenMinutes = jwt.AccessTokenMinutes;
    options.RefreshTokenDays = jwt.RefreshTokenDays;
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();

var environmentOrigins = builder.Configuration["CORS_ORIGINS"]?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var allowedOrigins = environmentOrigins is { Length: > 0 }
    ? environmentOrigins
    : builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("Spa", policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IProjectAccessService, ProjectAccessService>();
builder.Services.Configure<S3StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.AddSingleton<IAttachmentStorage>(services =>
    string.Equals(builder.Configuration["Storage:Provider"], "S3", StringComparison.OrdinalIgnoreCase)
        ? ActivatorUtilities.CreateInstance<S3AttachmentStorage>(services)
        : ActivatorUtilities.CreateInstance<LocalAttachmentStorage>(services));
builder.Services.AddHealthChecks();

var app = builder.Build();
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
if (!app.Environment.IsDevelopment())
{
    // Render termina TLS en su proxy y reenvía la solicitud al contenedor por HTTP.
    // El contenedor no está expuesto directamente a Internet, por lo que confiamos
    // en sus encabezados para evitar redirecciones HTTPS en bucle.
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
}
app.UseForwardedHeaders(forwardedHeadersOptions);
app.UseMiddleware<ExceptionMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskRino API v1"));
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();
app.UseCors("Spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var healthy = await db.Database.CanConnectAsync(cancellationToken);
    return healthy ? Results.Ok(new { status = "Healthy" }) : Results.Problem(statusCode: 503, title: "Database unavailable");
}).AllowAnonymous();

if (builder.Configuration.GetValue("Database:ApplyMigrations", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    await DbSeeder.SeedAsync(db, hasher);
}

app.Run();

public partial class Program;
