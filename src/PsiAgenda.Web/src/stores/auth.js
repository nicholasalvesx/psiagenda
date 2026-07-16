import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { api, lerToken, guardarToken, limparToken } from '../api/client'

/** Le as claims do payload do JWT. Serve so para a UI decidir o que mostrar. */
function lerClaims(token) {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]))
    return {
      nome: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || '',
      role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || '',
      expiraEm: payload.exp ? payload.exp * 1000 : 0,
    }
  } catch {
    return null
  }
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref(lerToken())

  // A UI usa essas claims por conveniencia; quem decide de verdade e sempre a API.
  const claims = computed(() => (token.value ? lerClaims(token.value) : null))
  const ehPsicologo = computed(() => claims.value?.role === 'Psicologo')
  const ehPaciente = computed(() => claims.value?.role === 'Paciente')
  const nome = computed(() => claims.value?.nome || '')

  // Sem checar validade: o access token dura 15 min e o interceptor renova sozinho.
  // Exigir 'nao expirado' aqui mandaria para o login quem so precisava de um refresh.
  const autenticado = computed(() => !!claims.value?.role)

  function guardar(accessToken) {
    token.value = accessToken
    guardarToken(accessToken)
  }

  function esquecer() {
    token.value = ''
    limparToken()
  }

  async function login(email, senha) {
    const { data } = await api.post('/auth/login', { email, senha })
    guardar(data.accessToken)
    return data
  }

  async function registrarPsicologo(dados) {
    const { data } = await api.post('/auth/registrar/psicologo', dados)
    guardar(data.accessToken)
    return data
  }

  async function registrarPaciente(dados) {
    const { data } = await api.post('/auth/registrar/paciente', dados)
    guardar(data.accessToken)
    return data
  }

  /** Revoga a cadeia de refresh no servidor; sem isso o cookie continuaria valendo. */
  async function sair() {
    try {
      await api.post('/auth/logout')
    } catch {
      // Se o servidor nao responder, ainda assim limpa o lado do cliente.
    }
    esquecer()
  }

  /** Recupera a sessao no F5: o access token pode ter expirado, mas o cookie sobrevive. */
  async function restaurarSessao() {
    if (token.value && claims.value && claims.value.expiraEm > Date.now()) return true

    try {
      const { data } = await api.post('/auth/refresh')
      guardar(data.accessToken)
      return true
    } catch {
      esquecer()
      return false
    }
  }

  /** Rota inicial de cada perfil: os dois paineis sao aplicacoes diferentes. */
  const rotaInicial = computed(() => {
    if (ehPsicologo.value) return '/painel/agenda'
    if (ehPaciente.value) return '/portal/consultas'
    return '/login'
  })

  return {
    token, autenticado, ehPsicologo, ehPaciente, nome, rotaInicial,
    login, registrarPsicologo, registrarPaciente, sair, esquecer, guardar, restaurarSessao,
  }
})
