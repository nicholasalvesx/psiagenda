<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Password from 'primevue/password'
import Button from 'primevue/button'
import Message from 'primevue/message'
import ProgressSpinner from 'primevue/progressspinner'
import { api, mensagemDeErro } from '../api/client'
import { useAuthStore } from '../stores/auth'

const rota = useRoute()
const router = useRouter()
const auth = useAuthStore()

const token = rota.query.token || ''
const verificando = ref(true)
const linkValido = ref(false)
const senha = ref('')
const confirmacao = ref('')
const erro = ref('')
const carregando = ref(false)
const pronto = ref(false)

onMounted(async () => {
  if (!token) {
    verificando.value = false
    return
  }

  try {
    const { data } = await api.get(`/auth/recuperar-senha/${encodeURIComponent(token)}`)
    linkValido.value = data.valido
  } catch {
    linkValido.value = false
  } finally {
    verificando.value = false
  }
})

async function redefinir() {
  erro.value = ''

  if (senha.value !== confirmacao.value) {
    erro.value = 'As senhas nao conferem.'
    return
  }

  carregando.value = true
  try {
    await api.post('/auth/redefinir-senha', { token, novaSenha: senha.value })

    // Trocar a senha derruba as sessoes no servidor; limpar aqui evita a tela
    // ficar com um token que ja nao vale.
    auth.esquecer()
    pronto.value = true
    setTimeout(() => router.push('/login'), 2500)
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Nao foi possivel redefinir a senha.')
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <div class="tela-acesso">
    <div class="cartao-acesso">
      <div class="marca">
        <i class="pi pi-lock" style="font-size: 2rem; color: var(--psi-azul)" />
        <h1>Nova senha</h1>
        <p v-if="!pronto">Escolha uma senha para sua conta</p>
      </div>

      <div v-if="verificando" class="vazio">
        <ProgressSpinner style="width: 36px; height: 36px" />
      </div>

      <template v-else-if="pronto">
        <Message severity="success" :closable="false">
          Senha alterada. Por seguranca, encerramos as sessoes abertas — entre novamente.
        </Message>
        <div class="rodape-acesso">
          <p><router-link to="/login">Ir para o login</router-link></p>
        </div>
      </template>

      <template v-else-if="!linkValido">
        <Message severity="error" :closable="false">
          Este link e invalido, ja foi usado ou expirou. Peca a recuperacao de novo.
        </Message>
        <div class="rodape-acesso">
          <p><router-link to="/recuperar-senha">Pedir novo link</router-link></p>
        </div>
      </template>

      <template v-else>
        <div v-if="erro" class="aviso-erro">{{ erro }}</div>

        <form @submit.prevent="redefinir">
          <div class="campo">
            <label for="senha">Nova senha</label>
            <Password v-model="senha" :feedback="false" toggle-mask fluid input-id="senha" required />
            <small class="dica">Minimo de 8 caracteres.</small>
          </div>

          <div class="campo">
            <label for="confirmacao">Repita a senha</label>
            <Password v-model="confirmacao" :feedback="false" toggle-mask fluid input-id="confirmacao" required />
          </div>

          <Button type="submit" label="Salvar nova senha" icon="pi pi-check" :loading="carregando" fluid />
        </form>
      </template>
    </div>
  </div>
</template>

<style scoped>
.dica {
  color: var(--psi-texto-suave);
  font-size: 0.75rem;
}
</style>
