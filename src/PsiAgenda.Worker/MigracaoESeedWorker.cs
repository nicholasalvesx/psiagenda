using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PsiAgenda.Application.Common;
using PsiAgenda.Infrastructure.Identity;
using PsiAgenda.Infrastructure.Persistencia;

namespace PsiAgenda.Worker;

/// <summary>
/// Job de bootstrap do banco: aplica migrations e garante as roles. Roda uma vez e encerra.
/// Fica fora da API de proposito — com varias replicas, migrar no start da API vira corrida.
/// </summary>
public class MigracaoESeedWorker(
    IServiceProvider provedor,
    IHostApplicationLifetime cicloDeVida,
    ILogger<MigracaoESeedWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try
        {
            using var escopo = provedor.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<AppDbContext>();

            logger.LogInformation("Aplicando migrations...");
            await db.Database.MigrateAsync(ct);

            var pendentes = await db.Database.GetPendingMigrationsAsync(ct);
            logger.LogInformation("Migrations aplicadas. Pendentes agora: {Qtd}", pendentes.Count());

            await SemearPerfisAsync(escopo.ServiceProvider, ct);

            logger.LogInformation("Bootstrap do banco concluido.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha no bootstrap do banco.");
            Environment.ExitCode = 1;
        }
        finally
        {
            cicloDeVida.StopApplication();
        }
    }

    private async Task SemearPerfisAsync(IServiceProvider servicos, CancellationToken ct)
    {
        var roleManager = servicos.GetRequiredService<RoleManager<PerfilApp>>();

        foreach (var role in RolesDoSistema.Todas)
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var resultado = await roleManager.CreateAsync(new PerfilApp(role));
            if (!resultado.Succeeded)
                throw new InvalidOperationException(
                    $"Nao foi possivel criar o perfil '{role}': {string.Join(" ", resultado.Errors.Select(e => e.Description))}");

            logger.LogInformation("Perfil '{Role}' criado.", role);
        }
    }
}
