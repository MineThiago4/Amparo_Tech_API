using Amparo_Tech_API.Data;
using Amparo_Tech_API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Exibir detalhes (PII) de erros do IdentityModel em ambiente de desenvolvimento
if (builder.Environment.IsDevelopment())
{
    IdentityModelEventSource.ShowPII = true;
}

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Amparo_Tech_API", Version = "v1" });

    // Definição do esquema de segurança JWT Bearer
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe apenas o token JWT. O prefixo 'Bearer' será adicionado automaticamente.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddControllers();

// CORS: permitir painel PHP local (qualquer porta em localhost/loopback)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPanel", policy =>
        policy
            .SetIsOriginAllowed(origin =>
            {
                try { var u = new Uri(origin); return u.IsLoopback; } catch { return false; }
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
        );

    // CORS para o app MAUI (mantido)
    options.AddPolicy("AllowMaui", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Registra servi?o de criptografia de mensagens
builder.Services.AddSingleton<IMessageCryptoService, AesGcmMessageCryptoService>();

// Registra política de mídia (centraliza limites de upload e validações)
builder.Services.AddSingleton<IMediaPolicyService, DefaultMediaPolicyService>();

// Registra serviço de contexto do usuário (centraliza claims e perfis)
builder.Services.AddSingleton<IUserContextService, DefaultUserContextService>();

// Rate limiting (configur?vel)
var rlPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:PermitLimit") ?? 1000; // padr?o alto
var rlWindowSec = builder.Configuration.GetValue<int?>("RateLimiting:WindowSeconds") ?? 60;
var rlQueueLimit = builder.Configuration.GetValue<int?>("RateLimiting:QueueLimit") ?? 0;
var rlQueueProc = builder.Configuration.GetValue<QueueProcessingOrder>("RateLimiting:QueueProcessingOrder");
if (rlQueueProc == 0) rlQueueProc = QueueProcessingOrder.OldestFirst;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = rlPermitLimit;
        options.Window = TimeSpan.FromSeconds(rlWindowSec);
        options.QueueLimit = rlQueueLimit;
        options.QueueProcessingOrder = rlQueueProc;
    });

    // Pol?tica padr?o
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rlPermitLimit,
                Window = TimeSpan.FromSeconds(rlWindowSec),
                QueueProcessingOrder = rlQueueProc,
                QueueLimit = rlQueueLimit
            }));
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = true;
        o.IncludeErrorDetails = true; // inclui detalhes no desafio WWW-Authenticate e eventos
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado")))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Importante: não redirecionar HTTPS em desenvolvimento para evitar falha no preflight de CORS
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// CORS deve vir antes de Auth/Authorization
app.UseCors("AllowPanel");
// opcional: se o app MAUI também for utilizado em paralelo
// app.UseCors("AllowMaui");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("fixed");

app.Run();