<script setup>
import { ref } from 'vue'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { api, mensagemDeErro } from '../api/client'

const email = ref('')
const erro = ref('')
const carregando = ref(false)
const enviado = ref(false)

async function pedir() {
  erro.value = ''
  carregando.value = true
  try {
    await api.post('/auth/recuperar-senha', { email: email.value })
    // A mensagem e a mesma exista ou nao a conta: a tela nao pode denunciar quem tem cadastro.
    enviado.value = true
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Nao foi possivel enviar o e-mail.')
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <div class="tela-acesso">
    <div class="cartao-acesso">
      <div class="marca">
        <i class="pi pi-key" style="font-size: 2rem; color: var(--psi-azul)" />
        <h1>Esqueci minha senha</h1>
        <p>Enviamos um link para voce criar uma nova</p>
      </div>

      <template v-if="enviado">
        <Message severity="success" :closable="false">
          Se existir uma conta com <strong>{{ email }}</strong>, o link de recuperacao ja esta a caminho.
          Ele vale por 30 minutos.
        </Message>
        <div class="rodape-acesso">
          <p><router-link to="/login">Voltar para o login</router-link></p>
        </div>
      </template>

      <template v-else>
        <div v-if="erro" class="aviso-erro">{{ erro }}</div>

        <form @submit.prevent="pedir">
          <div class="campo">
            <label for="email">E-mail da conta</label>
            <InputText id="email" v-model="email" type="email" required autocomplete="username" />
          </div>

          <Button type="submit" label="Enviar link" icon="pi pi-send" :loading="carregando" fluid />
        </form>

        <div class="rodape-acesso">
          <p>Lembrou? <router-link to="/login">Entrar</router-link></p>
        </div>
      </template>
    </div>
  </div>
</template>
