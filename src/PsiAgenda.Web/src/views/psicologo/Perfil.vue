<script setup>
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import Select from 'primevue/select'
import DatePicker from 'primevue/datepicker'
import Message from 'primevue/message'
import Tag from 'primevue/tag'
import { api, mensagemDeErro } from '../../api/client'

const toast = useToast()

const perfil = ref(null)
const salvando = ref(false)
const form = ref({ nomeCompleto: '', duracaoPadraoConsultaMinutos: 50, fusoHorario: 'America/Sao_Paulo' })
const novaJanela = ref({ diaSemana: 1, horaInicio: null, horaFim: null })

const DIAS = [
  { label: 'Domingo', value: 0 }, { label: 'Segunda', value: 1 }, { label: 'Terca', value: 2 },
  { label: 'Quarta', value: 3 }, { label: 'Quinta', value: 4 }, { label: 'Sexta', value: 5 },
  { label: 'Sabado', value: 6 },
]

const FUSOS = [
  'America/Sao_Paulo', 'America/Manaus', 'America/Cuiaba', 'America/Belem',
  'America/Fortaleza', 'America/Recife', 'America/Rio_Branco', 'America/Noronha',
]

const nomeDoDia = (v) => DIAS.find((d) => d.value === v)?.label || v

function aplicar(dados) {
  perfil.value = dados
  form.value = {
    nomeCompleto: dados.nomeCompleto,
    duracaoPadraoConsultaMinutos: dados.duracaoPadraoConsultaMinutos,
    fusoHorario: dados.fusoHorario,
  }
}

async function carregar() {
  try {
    const { data } = await api.get('/perfil')
    aplicar(data)
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
  }
}

async function salvar() {
  salvando.value = true
  try {
    const { data } = await api.put('/perfil', form.value)
    aplicar(data)
    toast.add({ severity: 'success', summary: 'Perfil atualizado', life: 3000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
  } finally {
    salvando.value = false
  }
}

async function confirmarEPsi() {
  try {
    const { data } = await api.post('/perfil/epsi')
    aplicar(data)
    toast.add({ severity: 'success', summary: 'Atendimento online liberado', life: 3000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
  }
}

/** O backend recebe TimeOnly: 'HH:mm:ss'. */
const paraHora = (d) =>
  `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:00`

async function adicionarJanela() {
  if (!novaJanela.value.horaInicio || !novaJanela.value.horaFim) {
    toast.add({ severity: 'warn', summary: 'Informe o horario de inicio e fim', life: 4000 })
    return
  }
  try {
    const { data } = await api.post('/perfil/disponibilidades', {
      diaSemana: novaJanela.value.diaSemana,
      horaInicio: paraHora(novaJanela.value.horaInicio),
      horaFim: paraHora(novaJanela.value.horaFim),
    })
    aplicar(data)
    novaJanela.value = { diaSemana: 1, horaInicio: null, horaFim: null }
    toast.add({ severity: 'success', summary: 'Disponibilidade adicionada', life: 3000 })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
  }
}

async function removerJanela(id) {
  try {
    const { data } = await api.delete(`/perfil/disponibilidades/${id}`)
    aplicar(data)
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
  }
}

onMounted(carregar)
</script>

<template>
  <div v-if="perfil" class="pagina">
    <h2 class="titulo-pagina">Perfil</h2>
    <p class="subtitulo-pagina">Seus dados, horarios de atendimento e liberacao do online</p>

    <Message v-if="!perfil.cadastroEPsiAtivo" severity="warn" :closable="false" class="mb-2">
      <div class="aviso-epsi">
        <span>
          Para atender online, a Res. CFP 11/2018 exige cadastro previo no e-Psi.
          Confirme que seu cadastro esta ativo para liberar as consultas online.
        </span>
        <Button label="Ja tenho cadastro no e-Psi" size="small" @click="confirmarEPsi" />
      </div>
    </Message>
    <Message v-else severity="success" :closable="false" class="mb-2">
      Atendimento online liberado (e-Psi confirmado).
    </Message>

    <div class="cartao mb-2">
      <h3 class="secao">Dados profissionais</h3>

      <div class="grade-2">
        <div class="campo">
          <label>Nome completo</label>
          <InputText v-model="form.nomeCompleto" fluid />
        </div>
        <div class="campo">
          <label>CRP</label>
          <InputText :model-value="perfil.crp" disabled fluid />
        </div>
      </div>

      <div class="grade-2">
        <div class="campo">
          <label>Duracao padrao da consulta (min)</label>
          <InputNumber v-model="form.duracaoPadraoConsultaMinutos" :min="15" :max="240" fluid />
        </div>
        <div class="campo">
          <label>Fuso horario</label>
          <Select v-model="form.fusoHorario" :options="FUSOS" fluid />
        </div>
      </div>

      <Button label="Salvar" icon="pi pi-check" :loading="salvando" @click="salvar" />
    </div>

    <div class="cartao">
      <h3 class="secao">Disponibilidade semanal</h3>
      <p class="ajuda">Horarios em que voce aceita agendamentos. Consultas fora dessas janelas sao recusadas.</p>

      <div v-if="!perfil.disponibilidades.length" class="vazio">Nenhuma janela cadastrada.</div>

      <div v-else class="janelas">
        <div v-for="d in perfil.disponibilidades" :key="d.id" class="janela">
          <Tag :value="nomeDoDia(d.diaSemana)" severity="info" />
          <span>{{ d.horaInicio.slice(0, 5) }} as {{ d.horaFim.slice(0, 5) }}</span>
          <Button icon="pi pi-times" text rounded size="small" severity="danger"
                  @click="removerJanela(d.id)" aria-label="Remover" />
        </div>
      </div>

      <div class="nova-janela">
        <div class="campo">
          <label>Dia</label>
          <Select v-model="novaJanela.diaSemana" :options="DIAS" option-label="label" option-value="value" />
        </div>
        <div class="campo">
          <label>Inicio</label>
          <DatePicker v-model="novaJanela.horaInicio" time-only />
        </div>
        <div class="campo">
          <label>Fim</label>
          <DatePicker v-model="novaJanela.horaFim" time-only />
        </div>
        <Button label="Adicionar" icon="pi pi-plus" outlined @click="adicionarJanela" />
      </div>
    </div>
  </div>
</template>

<style scoped>
.secao {
  margin: 0 0 1rem;
  font-size: 1rem;
  color: var(--psi-azul-escuro);
}

.ajuda {
  color: var(--psi-texto-suave);
  font-size: 0.85rem;
  margin: -0.5rem 0 1rem;
}

.mb-2 {
  margin-bottom: 1rem;
}

.grade-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

.aviso-epsi {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.janelas {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.janela {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.5rem 0.75rem;
  background: var(--psi-azul-claro);
  border-radius: 8px;
}

.nova-janela {
  display: flex;
  align-items: flex-end;
  gap: 0.75rem;
  flex-wrap: wrap;
  border-top: 1px solid var(--psi-borda);
  padding-top: 1rem;
}

.nova-janela .campo {
  margin-bottom: 0;
}
</style>
