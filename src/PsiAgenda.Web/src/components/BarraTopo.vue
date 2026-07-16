<script setup>
import { useRouter } from 'vue-router'
import Button from 'primevue/button'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()

async function sair() {
  await auth.sair()
  router.push('/login')
}
</script>

<template>
  <header class="barra">
    <div class="barra-conteudo">
      <div class="barra-marca">
        <i class="pi pi-heart-fill" />
        <span>PsiAgenda</span>
      </div>

      <nav v-if="auth.ehPsicologo" class="barra-links">
        <router-link to="/painel/agenda">Agenda</router-link>
        <router-link to="/painel/pacientes">Pacientes</router-link>
        <router-link to="/painel/perfil">Perfil</router-link>
      </nav>
      <nav v-else class="barra-links">
        <router-link to="/portal/consultas">Minhas consultas</router-link>
      </nav>

      <div class="barra-usuario">
        <span class="barra-nome">{{ auth.nome }}</span>
        <Button label="Sair" icon="pi pi-sign-out" text severity="secondary" size="small" @click="sair" />
      </div>
    </div>
  </header>
</template>

<style scoped>
.barra {
  background: var(--psi-azul);
  color: #fff;
}

.barra-conteudo {
  max-width: 1100px;
  margin: 0 auto;
  padding: 0.6rem 1rem;
  display: flex;
  align-items: center;
  gap: 1.5rem;
  flex-wrap: wrap;
}

.barra-marca {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 600;
  font-size: 1.05rem;
}

.barra-links {
  display: flex;
  gap: 1rem;
  flex: 1;
}

.barra-links a {
  color: #dbeafe;
  text-decoration: none;
  font-size: 0.9rem;
  padding: 0.3rem 0;
  border-bottom: 2px solid transparent;
}

.barra-links a.router-link-active {
  color: #fff;
  border-bottom-color: #fff;
}

.barra-usuario {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.barra-nome {
  font-size: 0.85rem;
  color: #dbeafe;
}

.barra-usuario :deep(.p-button) {
  color: #fff;
}
</style>
