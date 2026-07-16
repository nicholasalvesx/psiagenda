using Microsoft.Extensions.DependencyInjection;
using PsiAgenda.Application.Agendamentos;
using PsiAgenda.Application.Auth;
using PsiAgenda.Application.Pacientes;
using PsiAgenda.Application.Psicologos;

namespace PsiAgenda.Application;

public static class InjecaoDeDependencia
{
    public static IServiceCollection AddAplicacao(this IServiceCollection services)
    {
        services.AddScoped<ServicoDeAutenticacao>();
        services.AddScoped<ServicoDeRecuperacaoDeSenha>();
        services.AddScoped<ServicoDeAgendamento>();
        services.AddScoped<ServicoDePacientes>();
        services.AddScoped<ServicoDeConvites>();
        services.AddScoped<ServicoDePerfil>();

        return services;
    }
}
