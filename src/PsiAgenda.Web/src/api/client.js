import axios from 'axios'

// withCredentials: o refresh token vive num cookie httpOnly e precisa acompanhar as chamadas.
export const api = axios.create({ baseURL: '/api', withCredentials: true })

const CHAVE_TOKEN = 'psiagenda_token'

export const lerToken = () => localStorage.getItem(CHAVE_TOKEN) || ''
export const guardarToken = (t) => localStorage.setItem(CHAVE_TOKEN, t)
export const limparToken = () => localStorage.removeItem(CHAVE_TOKEN)

api.interceptors.request.use((config) => {
  const token = lerToken()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let aoEncerrarSessao = null
export function definirCallbackDeExpiracao(fn) {
  aoEncerrarSessao = fn
}

/** Rotas que nao podem tentar refresh: renovar aqui seria recursao ou nao faz sentido. */
const SEM_REFRESH = ['/auth/refresh', '/auth/login', '/auth/logout', '/auth/registrar']

// Uma renovacao por vez: se cinco chamadas tomarem 401 juntas, cinco POSTs /refresh
// concorrentes usariam o mesmo cookie e o backend trataria como reuso de token,
// derrubando a sessao. Todas esperam a mesma promessa.
let renovacaoEmAndamento = null

function renovar() {
  renovacaoEmAndamento ??= api
    .post('/auth/refresh')
    .then(({ data }) => {
      guardarToken(data.accessToken)
      return data.accessToken
    })
    .finally(() => {
      renovacaoEmAndamento = null
    })

  return renovacaoEmAndamento
}

api.interceptors.response.use(
  (resp) => resp,
  async (erro) => {
    const req = erro.config
    const ehAuth = SEM_REFRESH.some((r) => req?.url?.startsWith(r))

    if (erro.response?.status !== 401 || ehAuth || req?._jaTentouRenovar) {
      // Refresh negado tambem cai aqui: a sessao acabou de vez.
      if (erro.response?.status === 401 && req?.url?.startsWith('/auth/refresh')) aoEncerrarSessao?.()
      return Promise.reject(erro)
    }

    try {
      const novo = await renovar()
      req._jaTentouRenovar = true
      req.headers.Authorization = `Bearer ${novo}`
      return api(req)
    } catch {
      aoEncerrarSessao?.()
      return Promise.reject(erro)
    }
  },
)

/** A API responde erro de negocio como ProblemDetails; o 'detail' e a mensagem para o usuario. */
export function mensagemDeErro(erro, padrao = 'Nao foi possivel completar a operacao.') {
  return erro.response?.data?.detail || erro.response?.data?.title || padrao
}
