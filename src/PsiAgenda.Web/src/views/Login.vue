<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import InputText from 'primevue/inputtext'
import Password from 'primevue/password'
import Button from 'primevue/button'
import { useAuthStore } from '../stores/auth'
import { mensagemDeErro } from '../api/client'

const auth = useAuthStore()
const router = useRouter()
const rota = useRoute()

const email = ref('')
const senha = ref('')
const erro = ref('')
const carregando = ref(false)

async function entrar() {
  erro.value = ''
  carregando.value = true
  try {
    await auth.login(email.value, senha.value)
    // O destino depende da role que veio no token: cada perfil tem seu painel.
    router.push(rota.query.redirecionar || auth.rotaInicial)
  } catch (e) {
    erro.value = mensagemDeErro(e, 'Nao foi possivel entrar.')
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <div class="tela-acesso">
    <div class="cartao-acesso">
      <div class="marca">
        <i class="pi pi-heart-fill" style="font-size: 2rem; color: var(--psi-azul)" />
        <h1>PsiAgenda</h1>
        <p>Agenda e atendimento online para psicologos</p>
      </div>

      <div v-if="erro" class="aviso-erro">{{ erro }}</div>

      <form @submit.prevent="entrar">
        <div class="campo">
          <label for="email">E-mail</label>
          <InputText id="email" v-model="email" type="email" required autocomplete="username" />
        </div>

        <div class="campo">
          <label for="senha">Senha</label>
          <!-- input-id (nao id): o id iria para o wrapper e o label apontaria para fora do input. -->
          <Password v-model="senha" :feedback="false" toggle-mask fluid input-id="senha" required />
        </div>

        <div class="esqueci">
          <router-link to="/recuperar-senha">Esqueci minha senha</router-link>
        </div>

        <Button type="submit" label="Entrar" icon="pi pi-sign-in" :loading="carregando" fluid />
      </form>

      <div class="rodape-acesso">
        <p>Sou psicologo e quero <router-link to="/registrar/psicologo">criar minha conta</router-link></p>
        <p class="nota">Paciente: seu acesso chega por e-mail, no convite do seu psicologo.</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.nota {
  font-size: 0.8rem;
}

.esqueci {
  text-align: right;
  margin: -0.5rem 0 1rem;
}

.esqueci a {
  color: var(--psi-azul);
  font-size: 0.8rem;
  text-decoration: none;
}
</style>
