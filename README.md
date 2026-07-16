# PsiAgenda

SaaS de agendamento e atendimento online para psicólogos. Cada profissional tem sua conta
(1 conta = 1 profissional), com painel próprio, e cada paciente tem um portal separado.

## Arquitetura

```
src/
  PsiAgenda.Domain/          Entidades, value objects e regras de negócio (sem dependência de infra)
  PsiAgenda.Application/     Casos de uso, DTOs e contratos (interfaces implementadas na infra)
  PsiAgenda.Infrastructure/  EF Core (escrita) + Dapper (leitura) + Identity + migrations
  PsiAgenda.Api/             API REST, JWT, SignalR (signaling do WebRTC)
  PsiAgenda.Worker/          Job de migrations + seed das roles (roda e encerra)
  PsiAgenda.Web/             Vue 3 + PrimeVue (azul e branco)
```

O fluxo de dependência é `Domain ← Application ← Infrastructure ← Api/Worker`. O Domain não
referencia nada de infra.

### Decisões que valem explicação

- **EF Core escreve, Dapper lê.** Agendar é transacional e passa pelo agregado; a grade da agenda
  é leitura pura e vai em SQL direto, sem change tracker.
- **`fim_utc` é coluna persistida**, embora derive de `inicio_utc + duração`. A constraint `EXCLUDE`
  do Postgres exige expressão `IMMUTABLE`, e `timestamptz + interval` é apenas `STABLE`.
- **A trava contra dupla marcação é do banco.** A checagem em `ServicoDeAgendamento` consulta e
  depois insere; dois pedidos simultâneos passariam pelos dois checks. Quem garante é a constraint
  `ck_agendamentos_sem_sobreposicao`.
- **Disponibilidade é hora local, agendamento é UTC.** Por isso o psicólogo tem `FusoHorario`.
- **O tenant viaja no JWT** (`psicologo_id`), nunca em parâmetro do cliente.
- **Access token curto (15 min) + refresh em cookie httpOnly.** O access token não é revogável, então
  a janela de estrago é pequena; o refresh nunca chega ao JavaScript, então um XSS não rouba a sessão.
  O refresh **rotaciona** a cada uso e um token reapresentado revoga a cadeia inteira (sinal de cópia
  roubada). O front serializa as renovações — cinco 401 simultâneos gerariam cinco `/refresh` com o
  mesmo cookie, e o backend leria isso como reuso.
- **O paciente só cria login com convite.** Se bastasse informar o e-mail, qualquer um poderia tomar
  a conta de um paciente cadastrado antes que ele mesmo a criasse. O convite prova posse da caixa de entrada.
- **O envio de e-mail não recebe o `CancellationToken` da requisição.** O paciente já está salvo quando
  o convite sai; se o envio morresse junto com a conexão do cliente (fechar a aba, navegar), o cadastro
  ficaria de pé e o link nunca chegaria. `IEnviadorDeEmail` não expõe token e usa timeout próprio.
- **Redefinir senha revoga todas as sessões.** Sem isso, o refresh do invasor que motivou a troca
  continuaria valendo por 14 dias — trocar a senha não adiantaria nada.
- **PrimeVue fixado em 4.5.5**: a partir da 5 a licença deixou de ser MIT e passa a exigir chave
  (paga acima de US$ 1M de receita, 5 devs ou 10 funcionários).

## Rodando

### 1. Banco

```bash
docker compose up -d postgres
```

> Neste ambiente o contexto ativo do Docker aponta para o Docker Desktop, que não está rodando.
> Use `docker --context default compose up -d postgres`.

O Postgres sobe na porta **5433** (para não conflitar com uma instalação local na 5432).

### 2. Migrations e seed

```bash
export DOTNET_ROOT=/home/nicholas/.dotnet   # o /usr/bin/dotnet não enxerga os SDKs do home
export PATH=$DOTNET_ROOT:$PATH

dotnet run --project src/PsiAgenda.Worker
```

Aplica as migrations e cria as roles `Psicologo` e `Paciente`. Roda uma vez e encerra.

### 3. API

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 \
  dotnet run --project src/PsiAgenda.Api
```

Swagger em http://localhost:5080/swagger.

### 4. Front

```bash
cd src/PsiAgenda.Web
npm install
npm run dev
```

http://localhost:5173 (o Vite já faz proxy de `/api` e `/hubs` para a porta 5080).

## Vídeo (WebRTC)

O áudio e o vídeo vão **P2P** entre psicólogo e paciente. O servidor só faz o *signaling*
(troca de SDP/ICE) via SignalR no hub `/hubs/sinalizacao`, e valida no servidor quem pode
entrar em cada sala — a sala abre 10 min antes e fecha no fim da consulta.

**O TURN não é opcional em produção.** Sem ele, ~15-20% das conexões (NAT simétrico, CGNAT,
rede corporativa) não fecham e o paciente vê tela preta. O `docker-compose.yml` já traz um
coturn para desenvolvimento:

```bash
docker --context default compose up -d coturn
```

Em produção, o coturn precisa de IP público, portas 3478 (TCP/UDP) + faixa de mídia UDP, e
credenciais efêmeras em vez do usuário fixo do dev.

## E-mail e convites

Cadastrar um paciente dispara o convite automaticamente. O link vale **7 dias**, serve **uma vez**, e
reenviar invalida o anterior. Se o SMTP falhar, o paciente é salvo assim mesmo e a tela avisa — o
psicólogo reenvia pelo ícone de envelope na lista.

**Recuperação de senha** usa o mesmo desenho: link opaco de uso único, válido por 30 minutos, e pedir
um novo invalida o anterior. O endpoint responde igual para e-mail existente ou não — responder
diferente transformaria a rota num verificador de quem tem conta. Redefinir derruba as sessões abertas
e destrava a conta se o lockout tiver disparado.

Em desenvolvimento os e-mails não saem da máquina: o **Mailpit** captura tudo.

```bash
docker --context default compose up -d mailpit
```

Caixa de entrada em http://localhost:8025.

## Pendências conhecidas

- **Segredos**: `Jwt:ChaveSecreta` está no `appsettings.Development.json` só para dev. Em produção
  precisa vir de secret manager — a API se recusa a subir com chave menor que 32 bytes.
- **LGPD**: dado de saúde é dado pessoal *sensível* (art. 5º, II). Falta log de acesso a
  prontuário, criptografia em repouso e política de retenção. Sessões não são gravadas — se um dia
  forem, exige consentimento específico e muda a arquitetura de vídeo (P2P não grava).
- **e-Psi**: o cadastro é auto-declarado pelo profissional; não há validação contra o CFP.
- **E-mail é enviado dentro da requisição**, sem fila. Um SMTP lento segura a resposta, e uma queda
  entre o commit e o envio perde o e-mail (o convite tem botão de reenvio; a recuperação, não).
  A saída de verdade é um outbox com job de entrega.
- **Refresh entre abas**: o front serializa renovações por aba. Duas abas tomando 401 no mesmo
  instante ainda poderiam disparar dois `/refresh` e cair na detecção de reuso, derrubando a sessão.
  Se aparecer na prática, a saída é um lock via BroadcastChannel ou uma janela de tolerância no backend.
- **Limpeza de tokens**: `refresh_tokens`, `convites_de_acesso` e `tokens_de_recuperacao` só crescem.
  Falta um job apagando registros expirados/revogados.
- **Sem rate limit** em `/auth/login` e `/auth/recuperar-senha`. O lockout do Identity segura força
  bruta por conta, mas nada impede varrer e-mails ou inundar alguém de e-mails de recuperação.
