<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useToast } from 'primevue/usetoast'
import { useConfirm } from 'primevue/useconfirm'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import { api, mensagemDeErro } from '../../api/client'
import { formatarDataHora, formatarHora, SEVERIDADE_STATUS } from '../../utils/datas'

const toast = useToast()
const confirm = useConfirm()
const router = useRouter()

const consultas = ref([])
const carregando = ref(false)

const proximas = computed(() =>
  consultas.value
    .filter((c) => new Date(c.fimUtc) >= new Date() && c.status !== 'Cancelado')
    .sort((a, b) => new Date(a.inicioUtc) - new Date(b.inicioUtc)),
)

const anteriores = computed(() =>
  consultas.value.filter((c) => !proximas.value.includes(c)),
)

async function carregar() {
  carregando.value = true
  try {
    const { data } = await api.get('/minhas-consultas')
    consultas.value = data
  } catch (e) {
    toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 5000 })
  } finally {
    carregando.value = false
  }
}

const podeEntrar = (c) =>
  c.modalidade === 'Online' &&
  ['Pendente', 'Confirmado'].includes(c.status) &&
  Date.now() >= new Date(c.inicioUtc).getTime() - 10 * 60 * 1000 &&
  Date.now() <= new Date(c.fimUtc).getTime()

// Espelha a regra do backend so para nao oferecer um botao que a API vai recusar.
const podeCancelar = (c) =>
  ['Pendente', 'Confirmado'].includes(c.status) &&
  new Date(c.inicioUtc).getTime() - Date.now() > 24 * 60 * 60 * 1000

function confirmarCancelamento(c) {
  confirm.require({
    message: `Cancelar sua consulta de ${formatarDataHora(c.inicioUtc)}?`,
    header: 'Cancelar consulta',
    icon: 'pi pi-exclamation-triangle',
    rejectLabel: 'Voltar',
    acceptLabel: 'Cancelar consulta',
    acceptProps: { severity: 'danger' },
    accept: async () => {
      try {
        await api.post(`/minhas-consultas/${c.id}/cancelar`, { motivo: null })
        toast.add({ severity: 'success', summary: 'Consulta cancelada', life: 3000 })
        await carregar()
      } catch (e) {
        toast.add({ severity: 'error', summary: 'Erro', detail: mensagemDeErro(e), life: 6000 })
      }
    },
  })
}

onMounted(carregar)
</script>

<template>
  <div class="pagina">
    <h2 class="titulo-pagina">Minhas consultas</h2>
    <p class="subtitulo-pagina">Acompanhe seus atendimentos</p>

    <div v-if="carregando" class="vazio"><ProgressSpinner style="width: 40px; height: 40px" /></div>

    <template v-else>
      <Message severity="info" :closable="false" class="mb-2">
        O cancelamento pelo portal exige 24h de antecedencia. Depois disso, fale com seu psicologo.
      </Message>

      <h3 class="secao">Proximas</h3>
      <div v-if="!proximas.length" class="cartao vazio">Nenhuma consulta agendada.</div>

      <div v-else class="lista">
        <div v-for="c in proximas" :key="c.id" class="cartao consulta">
          <div class="consulta-quando">
            <strong>{{ formatarDataHora(c.inicioUtc) }}</strong>
            <small>ate {{ formatarHora(c.fimUtc) }}</small>
          </div>

          <div class="consulta-tags">
            <Tag :value="c.status" :severity="SEVERIDADE_STATUS[c.status]" />
            <Tag :value="c.modalidade" severity="secondary" />
          </div>

          <div class="linha-botoes">
            <Button v-if="podeEntrar(c)" label="Entrar na consulta" icon="pi pi-video" size="small"
                    @click="router.push(`/sala/${c.id}`)" />
            <Button v-if="podeCancelar(c)" label="Cancelar" size="small" text severity="danger"
                    @click="confirmarCancelamento(c)" />
          </div>
        </div>
      </div>

      <h3 v-if="anteriores.length" class="secao">Historico</h3>
      <div v-if="anteriores.length" class="lista">
        <div v-for="c in anteriores" :key="c.id" class="cartao consulta esmaecido">
          <div class="consulta-quando">
            <strong>{{ formatarDataHora(c.inicioUtc) }}</strong>
          </div>
          <div class="consulta-tags">
            <Tag :value="c.status" :severity="SEVERIDADE_STATUS[c.status]" />
            <Tag :value="c.modalidade" severity="secondary" />
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.secao {
  font-size: 1rem;
  color: var(--psi-azul-escuro);
  margin: 1.5rem 0 0.75rem;
}

.mb-2 {
  margin-bottom: 1rem;
}

.lista {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.consulta {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex-wrap: wrap;
}

.consulta-quando {
  display: flex;
  flex-direction: column;
  flex: 1;
  min-width: 150px;
}

.consulta-quando small {
  color: var(--psi-texto-suave);
}

.consulta-tags {
  display: flex;
  gap: 0.35rem;
}

.esmaecido {
  opacity: 0.7;
}
</style>
