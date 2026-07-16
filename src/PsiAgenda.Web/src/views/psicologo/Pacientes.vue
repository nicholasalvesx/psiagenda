<script setup>
import { ref, onMounted } from 'vue'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'
import InputText from 'primevue/inputtext'
import DatePicker from 'primevue/datepicker'
import Tag from 'primevue/tag'
import { api, mensagemDeErro } from '../../api/client'
import { formatarData, formatarDataHora } from '../../utils/datas'

const toast = useToast()
const confirm = useConfirm()

const pacientes = ref([])
const carregando = ref(false)
const salvando = ref(false)
const dialogAberto = ref(false)
const busca = ref('')
const editando = ref(null)
const form = ref({ nomeCompleto: '', email: '', telefone: '', dataNascimento: null })

async function carregar() {
  carregando.value = true
  try {
    const { data } = await api.get('/pacientes', { params: { busca: busca.value || undefined } })
    pacientes.value = data
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
  } finally {
    carregando.value = false
  }
}

function abrirNovo() {
  editando.value = null
  form.value = { nomeCompleto: '', email: '', telefone: '', dataNascimento: null }
  dialogAberto.value = true
}

function abrirEdicao(p) {
  editando.value = p
  form.value = {
    nomeCompleto: p.nomeCompleto,
    email: p.email,
    telefone: p.telefone || '',
    dataNascimento: p.dataNascimento ? new Date(p.dataNascimento + 'T00:00:00') : null,
  }
  dialogAberto.value = true
}

/** A API espera DateOnly ('YYYY-MM-DD'); toISOString converteria para UTC e poderia voltar um dia. */
function paraDateOnly(data) {
  if (!data) return null
  const mes = String(data.getMonth() + 1).padStart(2, '0')
  const dia = String(data.getDate()).padStart(2, '0')
  return `${data.getFullYear()}-${mes}-${dia}`
}

async function salvar() {
  salvando.value = true
  const corpo = {
    nomeCompleto: form.value.nomeCompleto,
    telefone: form.value.telefone || null,
    dataNascimento: paraDateOnly(form.value.dataNascimento),
  }

  try {
    if (editando.value) {
      await api.put(`/pacientes/${editando.value.id}`, corpo)
      toast.add({ severity: 'success', summary: 'Paciente atualizado', life: 3000 })
    } else {
      const { data } = await api.post('/pacientes', { ...corpo, email: form.value.email })

      // O cadastro pode ter dado certo e o e-mail nao: avisar em vez de fingir que foi tudo bem.
      if (data.conviteEnviado) {
        toast.add({
          severity: 'success',
          summary: 'Paciente cadastrado',
          detail: `Convite enviado para ${form.value.email}.`,
          life: 4000,
        })
      } else {
        toast.add({
          severity: 'warn',
          summary: 'Paciente cadastrado, mas o convite nao saiu',
          detail: 'Use o botao de reenviar convite na lista.',
          life: 7000,
        })
      }
    }
    dialogAberto.value = false
    await carregar()
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 6000 })
  } finally {
    salvando.value = false
  }
}

async function reenviarConvite(p) {
  try {
    await api.post(`/pacientes/${p.id}/convite`)
    toast.add({
      severity: 'success',
      summary: 'Convite reenviado',
      detail: `Um novo link foi enviado para ${p.email}. O anterior deixou de valer.`,
      life: 5000,
    })
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 6000 })
  }
}

function confirmarDesativacao(p) {
  confirm.require({
    message: `Desativar ${p.nomeCompleto}? O historico de consultas continua guardado.`,
    header: 'Desativar paciente',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Voltar',
    acceptLabel: 'Desativar',
    acceptProps: { severity: 'danger' },
    accept: async () => {
      try {
        await api.delete(`/pacientes/${p.id}`)
        toast.add({ severity: 'success', summary: 'Paciente desativado', life: 3000 })
        await carregar()
      } catch (e) {
        toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
      }
    },
  })
}

onMounted(carregar)
</script>

<template>
  <div class="pagina">
    <h2 class="titulo-pagina">Pacientes</h2>
    <p class="subtitulo-pagina">Cadastre o e-mail do paciente para que ele possa criar o acesso ao portal</p>

    <div class="barra-acoes">
      <InputText v-model="busca" placeholder="Buscar por nome ou e-mail" @keyup.enter="carregar" />
      <div class="linha-botoes">
        <Button icon="pi pi-search" outlined @click="carregar" aria-label="Buscar" />
        <Button label="Novo paciente" icon="pi pi-plus" @click="abrirNovo" />
      </div>
    </div>

    <DataTable :value="pacientes" :loading="carregando" data-key="id" class="cartao" striped-rows>
      <template #empty>
        <div class="vazio">Nenhum paciente cadastrado ainda.</div>
      </template>

      <Column field="nomeCompleto" header="Nome" />
      <Column field="email" header="E-mail" />
      <Column field="telefone" header="Telefone">
        <template #body="{ data }">{{ data.telefone || '—' }}</template>
      </Column>
      <Column header="Nascimento">
        <template #body="{ data }">{{ data.dataNascimento ? formatarData(data.dataNascimento) : '—' }}</template>
      </Column>
      <Column header="Proxima consulta">
        <template #body="{ data }">{{ data.proximaConsultaUtc ? formatarDataHora(data.proximaConsultaUtc) : '—' }}</template>
      </Column>
      <Column header="Portal">
        <template #body="{ data }">
          <Tag :value="data.possuiAcessoAoPortal ? 'Ativo' : 'Sem acesso'"
               :severity="data.possuiAcessoAoPortal ? 'success' : 'secondary'" />
        </template>
      </Column>
      <Column header="">
        <template #body="{ data }">
          <div class="linha-botoes">
            <Button v-if="!data.possuiAcessoAoPortal && data.ativo" icon="pi pi-envelope" text rounded size="small"
                    v-tooltip.top="'Reenviar convite'" @click="reenviarConvite(data)" aria-label="Reenviar convite" />
            <Button icon="pi pi-pencil" text rounded size="small" @click="abrirEdicao(data)" aria-label="Editar" />
            <Button icon="pi pi-trash" text rounded size="small" severity="danger"
                    @click="confirmarDesativacao(data)" aria-label="Desativar" />
          </div>
        </template>
      </Column>
    </DataTable>

    <Dialog v-model:visible="dialogAberto" :header="editando ? 'Editar paciente' : 'Novo paciente'" modal
            :style="{ width: '440px' }">
      <div class="campo">
        <label>Nome completo</label>
        <InputText v-model="form.nomeCompleto" fluid />
      </div>

      <div class="campo">
        <label>E-mail</label>
        <InputText v-model="form.email" type="email" fluid :disabled="!!editando" />
        <small v-if="editando" style="color: var(--psi-texto-suave)">O e-mail e a chave de acesso e nao muda.</small>
      </div>

      <div class="campo">
        <label>Telefone</label>
        <InputText v-model="form.telefone" fluid />
      </div>

      <div class="campo">
        <label>Data de nascimento</label>
        <DatePicker v-model="form.dataNascimento" date-format="dd/mm/yy" fluid />
      </div>

      <template #footer>
        <Button label="Cancelar" text severity="secondary" @click="dialogAberto = false" />
        <Button label="Salvar" icon="pi pi-check" :loading="salvando" @click="salvar" />
      </template>
    </Dialog>
  </div>
</template>

<style scoped>
.barra-acoes {
  display: flex;
  justify-content: space-between;
  gap: 1rem;
  margin-bottom: 1rem;
  flex-wrap: wrap;
}
</style>
