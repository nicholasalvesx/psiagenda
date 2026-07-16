using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PsiAgenda.Application;
using PsiAgenda.Application.Agendamentos;
using PsiAgenda.Application.Auth;
using PsiAgenda.Application.Common;
using PsiAgenda.Application.Notificacoes;
using PsiAgenda.Application.Pacientes;
using PsiAgenda.Infrastructure.Notificacoes;
using PsiAgenda.Infrastructure.Seguranca;
using PsiAgenda.Domain.Agendamentos;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Pacientes;
using PsiAgenda.Domain.Psicologos;
using PsiAgenda.Infrastructure.Identity;
using PsiAgenda.Infrastructure.Persistencia;
using PsiAgenda.Infrastructure.Persistencia.Dapper;
using PsiAgenda.Infrastructure.Persistencia.Repositorios;

namespace PsiAgenda.Infrastructure;

public static class InjecaoDeDependencia
{
    public static IServiceCollection AddInfraestrutura(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("ConnectionStrings:Postgres nao configurada.");

        services.AddDbContext<AppDbContext>(opt => opt
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddIdentityCore<UsuarioApp>(opt =>
            {
                opt.Password.RequiredLength = 8;
                opt.Password.RequireNonAlphanumeric = false;
                opt.User.RequireUniqueEmail = true;

                opt.Lockout.MaxFailedAccessAttempts = 5;
                opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                opt.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<PerfilApp>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddErrorDescriber<DescritorDeErrosEmPortugues>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SecaoConfig));
        services.Configure<OpcoesDeEmail>(config.GetSection(OpcoesDeEmail.SecaoConfig));

        services.AddScoped<IPsicologoRepository, PsicologoRepository>();
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IAgendamentoRepository, AgendamentoRepository>();
        services.AddScoped<IConviteRepository, ConviteRepository>();
        services.AddScoped<IRepositorioDeRecuperacao, RepositorioDeRecuperacao>();

        services.AddSingleton<IConexaoFactory>(_ => new ConexaoFactory(connectionString));
        services.AddScoped<IAgendaQueries, AgendaQueries>();
        services.AddScoped<IPacienteQueries, PacienteQueries>();

        services.AddScoped<IGerenciadorDeUsuarios, GerenciadorDeUsuarios>();
        services.AddScoped<IGerenciadorDeRefreshToken, GerenciadorDeRefreshToken>();
        services.AddSingleton<IGeradorDeToken, GeradorDeToken>();
        services.AddSingleton<IGeradorDeTokenOpaco, GeradorDeTokenOpaco>();
        services.AddScoped<IEnviadorDeEmail, EnviadorDeEmailSmtp>();

        services.AddAplicacao();

        return services;
    }
}
