using Microsoft.AspNetCore.Identity;
using PsiAgenda.Application.Auth;
using PsiAgenda.Domain.Common;

namespace PsiAgenda.Infrastructure.Identity;

public class GerenciadorDeUsuarios(UserManager<UsuarioApp> userManager) : IGerenciadorDeUsuarios
{
    public async Task<Guid> CriarUsuarioAsync(string email, string senha, string role, CancellationToken ct = default)
    {
        var usuario = new UsuarioApp { UserName = email, Email = email, EmailConfirmed = false };

        var criacao = await userManager.CreateAsync(usuario, senha);
        if (!criacao.Succeeded)
            throw new DomainException(string.Join(" ", criacao.Errors.Select(e => e.Description)));

        var vinculo = await userManager.AddToRoleAsync(usuario, role);
        if (!vinculo.Succeeded)
        {
            await userManager.DeleteAsync(usuario);
            throw new DomainException(string.Join(" ", vinculo.Errors.Select(e => e.Description)));
        }

        return usuario.Id;
    }

    public async Task<Guid?> ValidarCredenciaisAsync(string email, string senha, CancellationToken ct = default)
    {
        var usuario = await userManager.FindByEmailAsync(email);
        if (usuario is null)
            return null;

        if (await userManager.IsLockedOutAsync(usuario))
            throw new DomainException("Conta temporariamente bloqueada por excesso de tentativas. Tente novamente em alguns minutos.");

        if (!await userManager.CheckPasswordAsync(usuario, senha))
        {
            // Alimenta o lockout: sem isso a senha fica aberta a forca bruta.
            await userManager.AccessFailedAsync(usuario);
            return null;
        }

        await userManager.ResetAccessFailedCountAsync(usuario);
        return usuario.Id;
    }

    public async Task<string?> ObterRoleAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is null)
            return null;

        var roles = await userManager.GetRolesAsync(usuario);
        return roles.FirstOrDefault();
    }

    public async Task<string?> ObterEmailAsync(Guid usuarioId, CancellationToken ct = default)
        => (await userManager.FindByIdAsync(usuarioId.ToString()))?.Email;

    public async Task<Guid?> ObterIdPorEmailAsync(string email, CancellationToken ct = default)
        => (await userManager.FindByEmailAsync(email))?.Id;

    public async Task RedefinirSenhaAsync(Guid usuarioId, string novaSenha, CancellationToken ct = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new DomainException("Usuario nao encontrado.");

        // Valida antes de tocar no hash: senha fraca nao pode deixar a conta em estado quebrado.
        foreach (var validador in userManager.PasswordValidators)
        {
            var resultado = await validador.ValidateAsync(userManager, usuario, novaSenha);
            if (!resultado.Succeeded)
                throw new DomainException(string.Join(" ", resultado.Errors.Select(e => e.Description)));
        }

        usuario.PasswordHash = userManager.PasswordHasher.HashPassword(usuario, novaSenha);

        var atualizacao = await userManager.UpdateAsync(usuario);
        if (!atualizacao.Succeeded)
            throw new DomainException(string.Join(" ", atualizacao.Errors.Select(e => e.Description)));

        await userManager.UpdateSecurityStampAsync(usuario);

        // Quem esqueceu a senha provavelmente errou ate travar a conta; redefinir destrava.
        await userManager.ResetAccessFailedCountAsync(usuario);
        if (await userManager.IsLockedOutAsync(usuario))
            await userManager.SetLockoutEndDateAsync(usuario, null);
    }

    public async Task<bool> EmailJaCadastradoAsync(string email, CancellationToken ct = default)
        => await userManager.FindByEmailAsync(email) is not null;

    public async Task RemoverUsuarioAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is not null)
            await userManager.DeleteAsync(usuario);
    }
}
