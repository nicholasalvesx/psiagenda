using PsiAgenda.Application.Common;
using PsiAgenda.Application.Pacientes;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Pacientes;
using PsiAgenda.Domain.Psicologos;

namespace PsiAgenda.Application.Auth;

public class ServicoDeAutenticacao(
    IGerenciadorDeUsuarios usuarios,
    IGeradorDeToken token,
    IGerenciadorDeRefreshToken refresh,
    ServicoDeConvites convites,
    IPsicologoRepository psicologos,
    IPacienteRepository pacientes,
    IUnitOfWork uow)
{
    public async Task<ParDeTokens> RegistrarPsicologoAsync(RegistrarPsicologoRequest req, CancellationToken ct = default)
    {
        var email = Email.Criar(req.Email);
        var crp = Crp.Criar(req.Crp);

        if (await usuarios.EmailJaCadastradoAsync(email.Valor, ct))
            throw new DomainException("Ja existe uma conta com esse e-mail.");
        if (await psicologos.ExisteComCrpAsync(crp.Valor, ct))
            throw new DomainException("Ja existe um profissional cadastrado com esse CRP.");

        var usuarioId = await usuarios.CriarUsuarioAsync(email.Valor, req.Senha, RolesDoSistema.Psicologo, ct);

        // Identity e o dominio vivem em transacoes distintas; sem esse rollback um erro aqui
        // deixaria um login orfao que bloquearia o e-mail para sempre.
        try
        {
            var psicologo = new Psicologo(usuarioId, req.NomeCompleto, email, crp);
            await psicologos.AdicionarAsync(psicologo, ct);
            await uow.SalvarAlteracoesAsync(ct);

            var acesso = token.Gerar(usuarioId, email.Valor, RolesDoSistema.Psicologo, psicologo.NomeCompleto, psicologo.Id, null);
            return new ParDeTokens(acesso, await refresh.EmitirAsync(usuarioId, ct: ct));
        }
        catch
        {
            await usuarios.RemoverUsuarioAsync(usuarioId, ct);
            throw;
        }
    }

    /// <summary>
    /// O paciente so cria login com um convite valido. O e-mail vem do convite, nao do formulario:
    /// se viesse do usuario, bastaria adivinhar o e-mail de um paciente para tomar a conta dele.
    /// </summary>
    public async Task<ParDeTokens> RegistrarPacienteAsync(RegistrarPacienteRequest req, CancellationToken ct = default)
    {
        var (convite, paciente) = await convites.ResolverAsync(req.Convite, ct);

        if (paciente.UsuarioId is not null)
            throw new DomainException("Esse paciente ja tem acesso ao portal.");
        if (await usuarios.EmailJaCadastradoAsync(paciente.Email.Valor, ct))
            throw new DomainException("Ja existe uma conta com esse e-mail.");

        var usuarioId = await usuarios.CriarUsuarioAsync(paciente.Email.Valor, req.Senha, RolesDoSistema.Paciente, ct);

        try
        {
            convite.Consumir();
            paciente.VincularUsuario(usuarioId);

            // Campos opcionais omitidos no cadastro do portal nao podem apagar o que o psicologo ja preencheu.
            paciente.AtualizarDados(
                req.NomeCompleto ?? paciente.NomeCompleto,
                req.Telefone ?? paciente.Telefone,
                req.DataNascimento ?? paciente.DataNascimento);
            await uow.SalvarAlteracoesAsync(ct);

            var acesso = token.Gerar(usuarioId, paciente.Email.Valor, RolesDoSistema.Paciente,
                paciente.NomeCompleto, paciente.PsicologoId, paciente.Id);
            return new ParDeTokens(acesso, await refresh.EmitirAsync(usuarioId, ct: ct));
        }
        catch
        {
            await usuarios.RemoverUsuarioAsync(usuarioId, ct);
            throw;
        }
    }

    public async Task<ParDeTokens> LoginAsync(LoginRequest req, CancellationToken ct = default)
    {
        var email = Email.Criar(req.Email);

        // Mensagem generica de proposito: nao revela se o e-mail existe.
        var usuarioId = await usuarios.ValidarCredenciaisAsync(email.Valor, req.Senha, ct)
            ?? throw new DomainException("E-mail ou senha invalidos.");

        var acesso = await MontarAcessoAsync(usuarioId, email.Valor, ct);
        return new ParDeTokens(acesso, await refresh.EmitirAsync(usuarioId, ct: ct));
    }

    /// <summary>Troca um refresh token valido por um par novo, sem pedir senha.</summary>
    public async Task<ParDeTokens> RenovarAsync(string refreshToken, CancellationToken ct = default)
    {
        var (usuarioId, novoRefresh) = await refresh.RotacionarAsync(refreshToken, ct);

        var email = await usuarios.ObterEmailAsync(usuarioId, ct)
            ?? throw new DomainException("Usuario nao encontrado.");

        // Reconsulta o perfil em vez de confiar no token anterior: conta desativada
        // tem que perder o acesso na primeira renovacao, nao so quando o refresh expirar.
        var acesso = await MontarAcessoAsync(usuarioId, email, ct);
        return new ParDeTokens(acesso, novoRefresh);
    }

    public Task EncerrarSessaoAsync(string refreshToken, CancellationToken ct = default)
        => refresh.RevogarAsync(refreshToken, ct);

    private async Task<TokenResponse> MontarAcessoAsync(Guid usuarioId, string email, CancellationToken ct)
    {
        var role = await usuarios.ObterRoleAsync(usuarioId, ct)
            ?? throw new DomainException("Usuario sem perfil de acesso definido.");

        return role switch
        {
            RolesDoSistema.Psicologo => await TokenDePsicologoAsync(usuarioId, email, ct),
            RolesDoSistema.Paciente => await TokenDePacienteAsync(usuarioId, email, ct),
            _ => throw new DomainException($"Perfil de acesso desconhecido: {role}")
        };
    }

    private async Task<TokenResponse> TokenDePsicologoAsync(Guid usuarioId, string email, CancellationToken ct)
    {
        var psicologo = await psicologos.ObterPorUsuarioIdAsync(usuarioId, ct)
            ?? throw new DomainException("Perfil de psicologo nao encontrado.");
        if (!psicologo.Ativo)
            throw new DomainException("Esta conta esta desativada.");

        return token.Gerar(usuarioId, email, RolesDoSistema.Psicologo, psicologo.NomeCompleto, psicologo.Id, null);
    }

    private async Task<TokenResponse> TokenDePacienteAsync(Guid usuarioId, string email, CancellationToken ct)
    {
        var paciente = await pacientes.ObterPorUsuarioIdAsync(usuarioId, ct)
            ?? throw new DomainException("Perfil de paciente nao encontrado.");
        if (!paciente.Ativo)
            throw new DomainException("Este cadastro esta inativo. Fale com seu psicologo.");

        return token.Gerar(usuarioId, email, RolesDoSistema.Paciente, paciente.NomeCompleto, paciente.PsicologoId, paciente.Id);
    }
}
