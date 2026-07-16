<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import InputNumber from 'primevue/inputnumber'
import InputText from 'primevue/inputtext'
import Tag from 'primevue/tag'
import ProgressSpinner from 'primevue/progressspinner'
import { api, mensagemDeErro } from '../../api/client'
import { formatarHora, formatarDia, inicioDaSemana, somarDias, SEVERIDADE_STATUS } from '../../utils/datas'

const toast = useToast()
const confirm = useConfirm()
const router = useRouter()

const semana = ref(inicioDaSemana(new Date()))
const consultas = ref([])
const pacientes = ref([])
const carregando = ref(false)
const salvando = ref(false)
const dialogAberto = ref(false)

const nova = ref({ pacienteId: null, data: null, hora: null, duracaoMinutos: 50, modalidade: 1, motivo: '' })

const fimDaSemana = computed(() => somarDias(semana.value, 7))

const rotuloSemana = computed(() => {
  const fim = somarDias(semana.value, 6)
  return `${semana.value.toLocaleDateString('pt-BR')} a ${fim.toLocaleDateString('pt-BR')}`
})

// Agrupa por dia local para a tela ficar legivel; a API devolve a lista plana em UTC.
const porDia = computed(() => {
  const grupos = new Map()
  for (const c of consultas.value) {
    const chave = new Date(c.inicioUtc).toDateString()
    if (!grupos.has(chave)) grupos.set(chave, [])
    grupos.get(chave).push(c)
  }
  return [...grupos.entries()].map(([, itens]) => ({ dia: itens[0].inicioUtc, itens }))
})

async function carregar() {
  carregando.value = true
  try {
    const { data } = await api.get('/agenda', {
      params: { deUtc: semana.value.toISOString(), ateUtc: fimDaSemana.value.toISOString() },
    })
    consultas.value = data
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
  } finally {
    carregando.value = false
  }
}

async function carregarPacientes() {
  const { data } = await api.get('/pacientes')
  pacientes.value = data
}

function mudarSemana(dias) {
  semana.value = somarDias(semana.value, dias)
  carregar()
}

function abrirDialog() {
  nova.value = { pacienteId: null, data: new Date(), hora: null, duracaoMinutos: 50, modalidade: 1, motivo: '' }
  dialogAberto.value = true
}

async function agendar() {
  if (!nova.value.pacienteId || !nova.value.data || !nova.value.hora) {
    toast.add({ severity: 'warn', summary: 'Campos obrigatorios', detail: 'Informe paciente, data e hora.', life: 4000 })
    return
  }

  // Junta data + hora escolhidas no fuso local e envia em UTC — a API recusa horario sem fuso.
  const inicio = new Date(nova.value.data)
  inicio.setHours(nova.value.hora.getHours(), nova.value.hora.getMinutes(), 0, 0)

  salvando.value = true
  try {
    await api.post('/agenda', {
      pacienteId: nova.value.pacienteId,
      inicioUtc: inicio.toISOString(),
      duracaoMinutos: nova.value.duracaoMinutos,
      modalidade: nova.value.modalidade,
      motivo: nova.value.motivo || null,
    })
    dialogAberto.value = false
    toast.add({ severity: 'success', summary: 'Consulta agendada', life: 3000 })
    semana.value = inicioDaSemana(inicio)
    await carregar()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Nao foi possivel agendar', detail: mensagemDeErro(e), life: 6000 })
  } finally {
    salvando.value = false
  }
}

async function acao(consulta, rota, mensagem) {
  try {
    await api.post(`/agenda/${consulta.id}/${rota}`, rota === 'cancelar' ? { motivo: null } : undefined)
    toast.add({ severity: 'success', summary: mensagem, life: 3000 })
    await carregar()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
  }
}

function confirmarCancelamento(consulta) {
  confirm.require({
    message: `Cancelar a consulta de ${consulta.pacienteNome}?`,
    header: 'Cancelar consulta',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Voltar',
    acceptLabel: 'Cancelar consulta',
    acceptProps: { severity: 'danger' },
    accept: () => acao(consulta, 'cancelar', 'Consulta cancelada'),
  })
}

const podeEntrar = (c) =>
  c.modalidade === 'Online' &&
  ['Pendente', 'Confirmado'].includes(c.status) &&
  Date.now() >= new Date(c.inicioUtc).getTime() - 10 * 60 * 1000 &&
  Date.now() <= new Date(c.fimUtc).getTime()

onMounted(async () => {
  await Promise.all([carregar(), carregarPacientes()])
})
</script>

<template>
  <div class="pagina">
    <h2 class="titulo-pagina">Agenda</h2>
    <p class="subtitulo-pagina">Suas consultas da semana</p>

    <div class="barra-acoes">
      <div class="navegador">
        <Button icon="pi pi-chevron-left" text rounded @click="mudarSemana(-7)" aria-label="Semana anterior" />
        <span class="rotulo-semana">{{ rotuloSemana }}</span>
        <Button icon="pi pi-chevron-right" text rounded @click="mudarSemana(7)" aria-label="Proxima semana" />
      </div>
      <Button label="Nova consulta" icon="pi pi-plus" @click="abrirDialog" />
    </div>

    <div v-if="carregando" class="vazio"><ProgressSpinner style="width: 40px; height: 40px" /></div>

    <div v-else-if="!consultas.length" class="cartao vazio">
      <i class="pi pi-calendar" style="font-size: 2rem; color: #cbd5e1" />
      <p>Nenhuma consulta nesta semana.</p>
    </div>

    <div v-else class="lista-dias">
      <div v-for="grupo in porDia" :key="grupo.dia" class="cartao">
        <h3 class="dia">{{ formatarDia(grupo.dia) }}</h3>

        <div v-for="c in grupo.itens" :key="c.id" class="consulta">
          <div class="consulta-hora">
            <strong>{{ formatarHora(c.inicioUtc) }}</strong>
            <small>{{ formatarHora(c.fimUtc) }}</small>
          </div>

          <div class="consulta-info">
            <span class="consulta-nome">{{ c.pacienteNome }}</span>
            <div class="consulta-tags">
              <Tag :value="c.status" :severity="SEVERIDADE_STATUS[c.status]" />
              <Tag :value="c.modalidade" severity="secondary" />
            </div>
          </div>

          <div class="linha-botoes">
            <Button v-if="podeEntrar(c)" label="Entrar" icon="pi pi-video" size="small"
                    @click="router.push(`/sala/${c.id}`)" />
            <Button v-if="c.status === 'Pendente'" label="Confirmar" icon="pi pi-check" size="small" outlined
                    @click="acao(c, 'confirmar', 'Consulta confirmada')" />
            <Button v-if="['Pendente', 'Confirmado'].includes(c.status)" label="Concluir" icon="pi pi-flag"
                    size="small" outlined severity="secondary" @click="acao(c, 'concluir', 'Consulta concluida')" />
            <Button v-if="['Pendente', 'Confirmado'].includes(c.status)" label="Falta" size="small" text
                    severity="secondary" @click="acao(c, 'falta', 'Falta registrada')" />
            <Button v-if="!['Cancelado', 'Concluido'].includes(c.status)" label="Cancelar" size="small" text
                    severity="danger" @click="confirmarCancelamento(c)" />
          </div>
        </div>
      </div>
    </div>

    <Dialog v-model:visible="dialogAberto" header="Nova consulta" modal :style="{ width: '480px' }">
      <div class="campo">
        <label>Paciente</label>
        <Select v-model="nova.pacienteId" :options="pacientes" option-label="nomeCompleto" option-value="id"
                placeholder="Selecione" filter fluid />
      </div>

      <div class="grade-2">
        <div class="campo">
          <label>Data</label>
          <DatePicker v-model="nova.data" date-format="dd/mm/yy" fluid />
        </div>
        <div class="campo">
          <label>Hora</label>
          <DatePicker v-model="nova.hora" time-only fluid />
        </div>
      </div>

      <div class="grade-2">
        <div class="campo">
          <label>Duracao (min)</label>
          <InputNumber v-model="nova.duracaoMinutos" :min="15" :max="240" fluid />
        </div>
        <div class="campo">
          <label>Modalidade</label>
          <Select v-model="nova.modalidade" :options="[
            { label: 'Online', value: 1 },
            { label: 'Presencial', value: 2 },
          ]" option-label="label" option-value="value" fluid />
        </div>
      </div>

      <div class="campo">
        <label>Observacao (opcional)</label>
        <InputText v-model="nova.motivo" fluid />
      </div>

      <template #footer>
        <Button label="Cancelar" text severity="secondary" @click="dialogAberto = false" />
        <Button label="Agendar" icon="pi pi-check" :loading="salvando" @click="agendar" />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
.barra-acoes {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}

.navegador {
  display: flex;
  align-items: center;
  gap: 0.25rem;
}

.rotulo-semana {
  font-weight: 500;
  min-width: 190px;
  text-align: center;
}

.lista-dias {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.dia {
  margin: 0 0 0.75rem;
  font-size: 0.95rem;
  color: var(--psi-azul-escuro);
  text-transform: capitalize;
}

.consulta {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem 0;
  border-top: 1px solid var(--psi-borda);
  flex-wrap: wrap;
}

.consulta-hora {
  display: flex;
  flex-direction: column;
  min-width: 60px;
}

.consulta-hora small {
  color: var(--psi-texto-suave);
}

.consulta-info {
  flex: 1;
  min-width: 160px;
}

.consulta-nome {
  display: block;
  font-weight: 500;
  margin-bottom: 0.3rem;
}

.consulta-tags {
  display: flex;
  gap: 0.35rem;
}

.grade-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}
</style>
