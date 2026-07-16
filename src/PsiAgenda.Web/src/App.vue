<script setup>
import Toast from 'primevue/toast'
import ConfirmDialog from 'primevue/confirmdialog'
import BarraTopo from './components/BarraTopo.vue'
import { useAuthStore } from './stores/auth'
import { useRoute } from 'vue-router'
import { computed } from 'vue'

const auth = useAuthStore()
const rota = useRoute()

// Login e cadastro tem layout proprio (faixa azul), sem a barra do app.
const mostrarBarra = computed(() => auth.autenticado && !rota.meta.publica)
</script>

<template>
  <BarraTopo v-if="mostrarBarra" />
  <router-view />
  <Toast position="top-right" />
  <ConfirmDialog />
</template>
