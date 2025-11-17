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
using Microsoft.AspNetCore.SignalR;
using Amparo_Tech_API.Hubs;

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

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Cole apenas o token JWT (sem o prefixo 'Bearer '). Ao usar 'Authorize' no Swagger, insira somente o token.",
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

// Registra serviço de criptografia de mensagens
builder.Services.AddSingleton<IMessageCryptoService, AesGcmMessageCryptoService>();

// Registra política de mídia (centraliza limites de upload e validações)
builder.Services.AddSingleton<IMediaPolicyService, DefaultMediaPolicyService>();

// Registra serviço de contexto do usuário (centraliza claims e perfis)
builder.Services.AddSingleton<IUserContextService, DefaultUserContextService>();

// Rate limiting (configurável)
var rlPermitLimit = builder.Configuration.GetValue<int?>("RateLimiting:PermitLimit") ?? 1000; // padrão alto
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

    // Política padrão
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
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"].FirstOrDefault();
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Register Notification service and SignalR
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSignalR();

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

// Normalize Authorization header to handle accidental double 'Bearer ' prefixes from Swagger UI copy/paste
app.Use(async (context, next) =>
{
    var auth = context.Request.Headers["Authorization"].ToString();
    if (!string.IsNullOrEmpty(auth))
    {
        // common mistakes: "Bearer Bearer <token>" or "BearerBearer <token>"
        if (auth.StartsWith("Bearer Bearer ", StringComparison.OrdinalIgnoreCase))
            context.Request.Headers["Authorization"] = "Bearer " + auth.Substring("Bearer Bearer ".Length);
        else if (auth.StartsWith("BearerBearer ", StringComparison.OrdinalIgnoreCase))
            context.Request.Headers["Authorization"] = "Bearer " + auth.Substring("BearerBearer ".Length);
        else if (auth.StartsWith("Bearer ") && auth.Count(c => c == ' ') > 1)
        {
            // collapse multiple spaces
            var parts = auth.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2 && parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                context.Request.Headers["Authorization"] = "Bearer " + parts[1];
        }
    }
    await next();
});

// CORS deve vir antes de Auth/Authorization
app.UseCors("AllowPanel");
// opcional: se o app MAUI também for utilizado em paralelo
// app.UseCors("AllowMaui");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireRateLimiting("fixed");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();