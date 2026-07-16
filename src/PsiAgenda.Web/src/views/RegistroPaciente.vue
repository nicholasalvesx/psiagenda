<script setup>
import { ref, onMounted } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import { useAuthStore } from '../stores/auth'
import { api, mensagemDeErro } from '../api/client'

const auth = useAuthStore()
const router = useRouter()
const rota = useRoute()

const convite = rota.query.convite || ''
const preview = ref(null)
const verificando = ref(true)
const conviteInvalido = ref('')

const form = ref({ nomeCompleto: '', telefone: '', senha: '' })
const erro = ref('')
const carregando = ref(false)

// O link do e-mail e a credencial: sem ele nao ha o que mostrar.
onMounted(async () => {
  if (!convite) {
    conviteInvalido.value = 'Este link nao tem um convite. Use o link que seu psicologo enviou por e-mail.'
    verificando.value = false
    return
  }

  try {
    const { data } = await api.get(`/auth/convite/${encodeURIComponent(convite)}`)
    preview.value = data
    form.value.nomeCompleto = data.nomeDoPaciente
  } catch (e) {
    conviteInvalido.value = mensagemDeErro(e, 'Convite invalido ou expirado.')
  } finally {
    verificando.value = false
  }
})

async function registrar() {
  erro.value = ''
  carregando.value = true
  try {
    await auth.registrarPaciente({
      convite,
      senha: form.value.senha,
      nomeCompleto: form.value.nomeCompleto,
      telefone: form.value.telefone || null,
    })
    router.push('/portal/consultas')
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Nao foi possivel criar o acesso.')
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <div class="tela-acesso">
    <div class="cartao-acesso">
      <div class="marca">
        <i class="pi pi-user" style="font-size: 2rem; color: var(--psi-azul)" />
        <h1>Acesso do paciente</h1>
        <p v-if="preview">Convite de {{ preview.nomeDoPsicologo }}</p>
        <p v-else>Crie sua senha para ver suas consultas</p>
      </div>

      <div v-if="verificando" class="vazio">
        <ProgressSpinner style="width: 36px; height: 36px" />
      </div>

      <template v-else-if="conviteInvalido">
        <Message severity="error" :closable="false">{{ conviteInvalido }}</Message>
        <div class="rodape-acesso">
          <p>Ja tem acesso? <router-link to="/login">Entrar</router-link></p>
        </div>
      </template>

      <template v-else>
        <Message severity="info" :closable="false" class="mb-3">
          Criando o acesso de <strong>{{ preview.email }}</strong>
        </Message>

        <div v-if="erro" class="aviso-erro">{{ erro }}</div>

        <form @submit.prevent="registrar">
          <div class="campo">
            <label for="nome">Nome completo</label>
            <InputText id="nome" v-model="form.nomeCompleto" required />
          </div>

          <div class="campo">
            <label for="telefone">Telefone (opcional)</label>
            <InputText id="telefone" v-model="form.telefone" />
          </div>

          <div class="campo">
            <label for="senha">Crie sua senha</label>
            <!-- feedback=false: o overlay do medidor cobre o botao de enviar e engole o clique. -->
            <Password v-model="form.senha" :feedback="false" toggle-mask fluid input-id="senha" required />
            <small class="dica">Minimo de 8 caracteres.</small>
          </div>

          <Button type="submit" label="Criar acesso" icon="pi pi-check" :loading="carregando" fluid />
        </form>

        <div class="rodape-acesso">
          <p>Ja tem acesso? <router-link to="/login">Entrar</router-link></p>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.mb-3 {
  margin-bottom: 1rem;
}

.dica {
  color: var(--psi-texto-suave);
  font-size: 0.75rem;
}
</style>
