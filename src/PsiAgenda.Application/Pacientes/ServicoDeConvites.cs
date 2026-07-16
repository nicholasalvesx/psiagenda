using Microsoft.Extensions.Options;
using PsiAgenda.Application.Common;
using PsiAgenda.Application.Notificacoes;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Pacientes;
using PsiAgenda.Domain.Psicologos;

namespace PsiAgenda.Application.Pacientes;

public record ConvitePreview(string NomeDoPaciente, string Email, string NomeDoPsicologo);

public class ServicoDeConvites(
    IConviteRepository convites,
    IPacienteRepository pacientes,
    IPsicologoRepository psicologos,
    IGeradorDeTokenOpaco tokens,
    IEnviadorDeEmail email,
    IOptions<OpcoesDeEmail> opcoes,
    IUnitOfWork uow)
{
    private readonly OpcoesDeEmail _opcoes = opcoes.Value;

    /// <summary>
    /// Emite (ou reemite) o convite e manda o e-mail. Nao persiste o token cru em lugar nenhum:
    /// depois deste metodo, so quem tem a caixa de entrada consegue usar o link.
    /// </summary>
    public async Task EmitirAsync(Guid psicologoId, Guid pacienteId, CancellationToken ct = default)
    {
        var paciente = await pacientes.ObterPorIdAsync(pacienteId, psicologoId, ct)
            ?? throw new DomainException("Paciente nao encontrado.");

        if (paciente.UsuarioId is not null)
            throw new DomainException("Esse paciente ja tem acesso ao portal.");
        if (!paciente.Ativo)
            throw new DomainException("Paciente inativo nao pode receber convite.");

        var psicologo = await psicologos.ObterPorIdAsync(psicologoId, ct)
            ?? throw new DomainException("Psicologo nao encontrado.");

        // So o link mais recente vale.
        foreach (var anterior in await convites.ObterEmAbertoAsync(pacienteId, ct))
            anterior.Cancelar();

        var token = tokens.Gerar();
        await convites.AdicionarAsync(new ConviteDeAcesso(pacienteId, tokens.Hash(token)), ct);
        await uow.SalvarAlteracoesAsync(ct);

        await email.EnviarAsync(MontarEmail(paciente, psicologo, token));
    }

    /// <summary>Dados para a tela de cadastro se apresentar antes do paciente digitar a senha.</summary>
    public async Task<ConvitePreview> ConsultarAsync(string token, CancellationToken ct = default)
    {
        var (_, paciente) = await ResolverAsync(token, ct);

        var psicologo = await psicologos.ObterPorIdAsync(paciente.PsicologoId, ct)
            ?? throw new DomainException("Psicologo nao encontrado.");

        return new ConvitePreview(paciente.NomeCompleto, paciente.Email.Valor, psicologo.NomeCompleto);
    }

    /// <summary>Valida o token e devolve o convite + paciente. Mensagem unica: nao diz se o token existe.</summary>
    public async Task<(ConviteDeAcesso Convite, Paciente Paciente)> ResolverAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException("Convite invalido.");

        var convite = await convites.ObterPorTokenHashAsync(tokens.Hash(token), ct)
            ?? throw new DomainException("Convite invalido ou ja utilizado.");

        if (!convite.Valido(DateTime.UtcNow))
            throw new DomainException("Convite invalido ou ja utilizado.");

        var paciente = await pacientes.ObterPorIdSemTenantAsync(convite.PacienteId, ct)
            ?? throw new DomainException("Convite invalido ou ja utilizado.");

        return (convite, paciente);
    }

    private MensagemDeEmail MontarEmail(Paciente paciente, Psicologo psicologo, string token)
    {
        var link = $"{_opcoes.UrlDoPortal.TrimEnd('/')}/registrar/paciente?convite={Uri.EscapeDataString(token)}";
        var primeiroNome = paciente.NomeCompleto.Split(' ')[0];

        var texto = $"""
            Ola, {primeiroNome}!

            {psicologo.NomeCompleto} criou um acesso para voce no PsiAgenda, onde voce pode ver suas
            consultas e entrar nos atendimentos online.

            Crie sua senha aqui: {link}

            O link vale por {ConviteDeAcesso.ValidadeEmDias} dias. Se voce nao esperava este convite, ignore este e-mail.
            """;

        var html = $"""
            <div style="font-family:system-ui,-apple-system,'Segoe UI',Roboto,sans-serif;max-width:520px;margin:0 auto;color:#0f172a">
              <div style="background:#1d4ed8;color:#fff;padding:20px;border-radius:12px 12px 0 0">
                <h1 style="margin:0;font-size:20px">PsiAgenda</h1>
              </div>
              <div style="border:1px solid #e2e8f0;border-top:0;border-radius:0 0 12px 12px;padding:24px">
                <p>Ola, <strong>{primeiroNome}</strong>!</p>
                <p>{psicologo.NomeCompleto} criou um acesso para voce no PsiAgenda, onde voce pode ver
                   suas consultas e entrar nos atendimentos online.</p>
                <p style="text-align:center;margin:28px 0">
                  <a href="{link}" style="background:#1d4ed8;color:#fff;text-decoration:none;padding:12px 24px;border-radius:8px;display:inline-block;font-weight:500">
                    Criar minha senha
                  </a>
                </p>
                <p style="color:#64748b;font-size:13px">
                  O link vale por {ConviteDeAcesso.ValidadeEmDias} dias. Se voce nao esperava este convite, ignore este e-mail.
                </p>
              </div>
            </div>
            """;

        return new MensagemDeEmail(paciente.Email.Valor, paciente.NomeCompleto,
            $"{psicologo.NomeCompleto} convidou voce para o PsiAgenda", html, texto);
    }
}
