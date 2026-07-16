import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const rotas = [
  { path: '/', redirect: '/login' },
  { path: '/login', component: () => import('../views/Login.vue'), meta: { publica: true } },
  { path: '/registrar/psicologo', component: () => import('../views/RegistroPsicologo.vue'), meta: { publica: true } },
  { path: '/registrar/paciente', component: () => import('../views/RegistroPaciente.vue'), meta: { publica: true } },
  { path: '/recuperar-senha', component: () => import('../views/RecuperarSenha.vue'), meta: { publica: true } },
  { path: '/redefinir-senha', component: () => import('../views/RedefinirSenha.vue'), meta: { publica: true } },

  // Painel do psicologo
  { path: '/painel/agenda', component: () => import('../views/psicologo/Agenda.vue'), meta: { role: 'Psicologo' } },
  { path: '/painel/pacientes', component: () => import('../views/psicologo/Pacientes.vue'), meta: { role: 'Psicologo' } },
  { path: '/painel/perfil', component: () => import('../views/psicologo/Perfil.vue'), meta: { role: 'Psicologo' } },

  // Portal do paciente
  { path: '/portal/consultas', component: () => import('../views/paciente/MinhasConsultas.vue'), meta: { role: 'Paciente' } },

  // Sala de video: as duas roles entram, a API decide quem pode
  { path: '/sala/:agendamentoId', component: () => import('../views/Sala.vue'), meta: { autenticado: true } },
]

export const router = createRouter({ history: createWebHistory(), routes: rotas })

router.beforeEach(async (para) => {
  const auth = useAuthStore()

  if (para.meta.publica) {
    // Quem ja esta logado nao volta para a tela de login.
    return auth.autenticado ? auth.rotaInicial : true
  }

  // Num F5 o access token pode ter expirado, mas o cookie de refresh sobrevive:
  // tenta recuperar a sessao antes de mandar o usuario para o login.
  if (!auth.autenticado && !(await auth.restaurarSessao()))
    return { path: '/login', query: { redirecionar: para.fullPath } }

  // Guard de UI apenas: impede o painel errado de aparecer. A autorizacao real e a da API.
  if (para.meta.role && para.meta.role !== (auth.ehPsicologo ? 'Psicologo' : 'Paciente'))
    return auth.rotaInicial

  return true
})
