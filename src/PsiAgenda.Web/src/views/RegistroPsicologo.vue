<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import { useAuthStore } from '../stores/auth'
import { mensagemDeErro } from '../api/client'

const auth = useAuthStore()
const router = useRouter()

const form = ref({ nomeCompleto: '', email: '', senha: '', crp: '' })
const erro = ref('')
const carregando = ref(false)

async function registrar() {
  erro.value = ''
  carregando.value = true
  try {
    await auth.registrarPsicologo(form.value)
    router.push('/painel/agenda')
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Nao foi possivel criar a conta.')
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <div class="tela-acesso">
    <div class="cartao-acesso">
      <div class="marca">
        <i class="pi pi-user-plus" style="font-size: 2rem; color: var(--psi-azul)" />
        <h1>Criar conta profissional</h1>
        <p>Sua agenda, seus pacientes, seus atendimentos</p>
      </div>

      <div v-if="erro" class="aviso-erro">{{ erro }}</div>

      <form @submit.prevent="registrar">
        <div class="campo">
          <label for="nome">Nome completo</label>
          <InputText id="nome" v-model="form.nomeCompleto" required />
        </div>

        <div class="campo">
          <label for="crp">CRP</label>
          <InputText id="crp" v-model="form.crp" placeholder="06/123456" required />
        </div>

        <div class="campo">
          <label for="email">E-mail</label>
          <InputText id="email" v-model="form.email" type="email" required autocomplete="username" />
        </div>

        <div class="campo">
          <label for="senha">Senha</label>
          <!-- feedback=false: o overlay do medidor cobre o botao de enviar e engole o clique. -->
          <Password v-model="form.senha" :feedback="false" toggle-mask fluid input-id="senha" required />
          <small class="dica">Minimo de 8 caracteres.</small>
        </div>

        <Button type="submit" label="Criar conta" icon="pi pi-check" :loading="carregando" fluid />
      </form>

      <div class="rodape-acesso">
        <p>Ja tem conta? <router-link to="/login">Entrar</router-link></p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dica {
  color: var(--psi-texto-suave);
  font-size: 0.75rem;
}
</style>
