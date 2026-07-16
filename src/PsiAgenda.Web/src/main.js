import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import { definePreset } from '@primeuix/themes'
import ToastService from 'primevue/toastservice'
import ConfirmationService from 'primevue/confirmationservice'
import Tooltip from 'primevue/tooltip'

import 'primeicons/primeicons.css'
import './assets/estilo.css'

import App from './App.vue'
import { router } from './router'
import { definirCallbackDeExpiracao } from './api/client'
import { useAuthStore } from './stores/auth'

// Azul e branco: o Aura ja e neutro, so trocamos a cor primaria pela paleta azul.
const TemaPsiAgenda = definePreset(Aura, {
  semantic: {
    primary: {
      50: '{blue.50}', 100: '{blue.100}', 200: '{blue.200}', 300: '{blue.300}',
      400: '{blue.400}', 500: '{blue.500}', 600: '{blue.600}', 700: '{blue.700}',
      800: '{blue.800}', 900: '{blue.900}', 950: '{blue.950}',
    },
  },
})

const app = createApp(App)

app.use(createPinia())
app.use(PrimeVue, {
  theme: { preset: TemaPsiAgenda, options: { darkModeSelector: '.modo-escuro' } },
  locale: {
    dayNamesMin: ['D', 'S', 'T', 'Q', 'Q', 'S', 'S'],
    monthNames: ['Janeiro', 'Fevereiro', 'Marco', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro'],
    monthNamesShort: ['Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun', 'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez'],
    firstDayOfWeek: 0,
  },
})
app.use(ToastService)
app.use(ConfirmationService)
app.use(router)
app.directive('tooltip', Tooltip)

// Refresh recusado (expirado, revogado ou reuso detectado): nao ha o que renovar, cai para o login.
definirCallbackDeExpiracao(() => {
  useAuthStore().esquecer()
  if (router.currentRoute.value.path !== '/login') router.push('/login')
})

app.mount('#app')
