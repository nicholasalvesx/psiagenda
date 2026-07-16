using Microsoft.Extensions.Logging;
using PsiAgenda.Domain.Common;
using PsiAgenda.Domain.Pacientes;

namespace PsiAgenda.Application.Pacientes;

public class ServicoDePacientes(
    IPacienteRepository pacientes,
    ServicoDeConvites convites,
    ILogger<ServicoDePacientes> logger,
    IUnitOfWork uow)
{
    public async Task<(Paciente Paciente, bool ConviteEnviado)> CadastrarAsync(
        Guid psicologoId, CadastrarPacienteRequest req, CancellationToken ct = default)
    {
        var email = Email.Criar(req.Email);

        if (await pacientes.ExisteComEmailAsync(email.Valor, psicologoId, ct))
            throw new DomainException("Voce ja tem um paciente cadastrado com esse e-mail.");

        var paciente = new Paciente(psicologoId, req.NomeCompleto, email, req.Telefone, req.DataNascimento);
        await pacientes.AdicionarAsync(paciente, ct);
        await uow.SalvarAlteracoesAsync(ct);

        // O paciente ja esta salvo: uma falha no SMTP nao pode desfazer o cadastro nem virar erro
        // na tela. O psicologo reenvia o convite pelo botao quando o e-mail voltar a funcionar.
        //
        // CancellationToken.None de proposito: com o token da requisicao, o psicologo que fecha a aba
        // logo depois de salvar cancela a emissao no meio, e o paciente fica sem nunca receber o link.
        try
        {
            await convites.EmitirAsync(psicologoId, paciente.Id, CancellationToken.None);
            return (paciente, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Paciente {Paciente} cadastrado, mas o convite nao pode ser enviado", paciente.Id);
            return (paciente, false);
        }
    }

    public async Task<Paciente> AtualizarAsync(Guid psicologoId, Guid pacienteId, AtualizarPacienteRequest req, CancellationToken ct = default)
    {
        var paciente = await pacientes.ObterPorIdAsync(pacienteId, psicologoId, ct)
            ?? throw new DomainException("Paciente nao encontrado.");

        paciente.AtualizarDados(req.NomeCompleto, req.Telefone, req.DataNascimento);
        await uow.SalvarAlteracoesAsync(ct);

        return paciente;
    }

    public async Task DesativarAsync(Guid psicologoId, Guid pacienteId, CancellationToken ct = default)
    {
        var paciente = await pacientes.ObterPorIdAsync(pacienteId, psicologoId, ct)
            ?? throw new DomainException("Paciente nao encontrado.");

        paciente.Desativar();
        await uow.SalvarAlteracoesAsync(ct);
    }
}
