using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PsiAgenda.Api.Autenticacao;
using PsiAgenda.Api.Middlewares;
using PsiAgenda.Api.Video;
using PsiAgenda.Application.Common;
using PsiAgenda.Infrastructure;
using PsiAgenda.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfraestrutura(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUsuarioAtual, UsuarioAtual>();
builder.Services.Configure<TurnOptions>(builder.Configuration.GetSection(TurnOptions.SecaoConfig));
builder.Services.AddExceptionHandler<TratamentoDeErros>();
builder.Services.AddProblemDetails();

var jwt = builder.Configuration.GetSection(JwtOptions.SecaoConfig).Get<JwtOptions>()
          ?? throw new InvalidOperationException("Secao 'Jwt' nao configurada.");

// Falhar no boot e melhor que subir a API assinando token com chave fraca.
if (string.IsNullOrWhiteSpace(jwt.ChaveSecreta) || Encoding.UTF8.GetByteCount(jwt.ChaveSecreta) < 32)
    throw new InvalidOperationException("Jwt:ChaveSecreta precisa de no minimo 32 bytes para HS256.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ChaveSecreta)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // WebSocket nao manda header Authorization: o SignalR envia o token na query string.
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = contexto =>
            {
                var token = contexto.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && contexto.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    contexto.Token = token;

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

const string PoliticaCors = "frontend";
builder.Services.AddCors(opt => opt.AddPolicy(PoliticaCors, p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origens").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo { Title = "PsiAgenda API", Version = "v1" });
    opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Cole apenas o token (sem 'Bearer ').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    opt.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(PoliticaCors);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<SinalizacaoHub>("/hubs/sinalizacao");
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();
