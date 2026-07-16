<script setup>
import { ref, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import * as signalR from '@microsoft/signalr'
import Button from 'primevue/button'
import Message from 'primevue/message'
import { api, mensagemDeErro } from '../api/client'
import { formatarHora } from '../utils/datas'

const rota = useRoute()
const router = useRouter()

const videoLocal = ref(null)
const videoRemoto = ref(null)

const sala = ref(null)
const estado = ref('Preparando...')
const erro = ref('')
const conectado = ref(false)
const microfoneLigado = ref(true)
const cameraLigada = ref(true)

let conexao = null
let peer = null
let midiaLocal = null

// A oferta parte de quem ja estava na sala quando o outro entrou. Sem essa regra,
// os dois lados ofertam ao mesmo tempo e a negociacao colide.
async function criarPeer() {
  peer = new RTCPeerConnection({ iceServers: sala.value.iceServers })

  midiaLocal.getTracks().forEach((track) => peer.addTrack(track, midiaLocal))

  peer.ontrack = (evento) => {
    if (videoRemoto.value) videoRemoto.value.srcObject = evento.streams[0]
    conectado.value = true
    estado.value = 'Conectado'
  }

  peer.onicecandidate = (evento) => {
    if (evento.candidate) enviar('candidate', JSON.stringify(evento.candidate))
  }

  peer.onconnectionstatechange = () => {
    // Fonte da verdade do rotulo: o ontrack pode disparar antes do fim da negociacao,
    // e uma atribuicao posterior sobrescreveria o 'Conectado'.
    if (peer.connectionState === 'connected') {
      conectado.value = true
      estado.value = 'Conectado'
    }
    if (peer.connectionState === 'failed') {
      // Quase sempre e NAT sem TURN alcancavel.
      erro.value = 'A conexao de video falhou. Verifique sua rede ou tente novamente.'
      estado.value = 'Falhou'
    }
    if (peer.connectionState === 'disconnected') estado.value = 'Reconectando...'
  }

  return peer
}

const enviar = (tipo, payload) => conexao?.invoke('EnviarSinal', sala.value.salaVideoId, tipo, payload)

async function iniciarOferta() {
  await criarPeer()
  const oferta = await peer.createOffer()
  await peer.setLocalDescription(oferta)
  await enviar('offer', JSON.stringify(oferta))
  estado.value = 'Chamando...'
}

async function tratarSinal(_de, tipo, payload) {
  const dados = JSON.parse(payload)

  if (tipo === 'offer') {
    estado.value = 'Conectando...'
    if (!peer) await criarPeer()
    await peer.setRemoteDescription(new RTCSessionDescription(dados))
    const resposta = await peer.createAnswer()
    await peer.setLocalDescription(resposta)
    await enviar('answer', JSON.stringify(resposta))
  } else if (tipo === 'answer') {
    await peer.setRemoteDescription(new RTCSessionDescription(dados))
  } else if (tipo === 'candidate' && peer) {
    try {
      await peer.addIceCandidate(new RTCIceCandidate(dados))
    } catch {
      // Candidato tardio ou duplicado nao invalida a chamada.
    }
  }
}

async function entrar() {
  try {
    estado.value = 'Validando acesso...'
    const { data } = await api.get(`/video/${rota.params.agendamentoId}/entrar`)
    sala.value = data

    estado.value = 'Ligando camera...'
    midiaLocal = await navigator.mediaDevices.getUserMedia({ video: true, audio: true })
    if (videoLocal.value) videoLocal.value.srcObject = midiaLocal

    estado.value = 'Conectando a sala...'
    conexao = new signalR.HubConnectionBuilder()
      // WebSocket nao manda header: o token vai por accessTokenFactory (query string).
      .withUrl('/hubs/sinalizacao', { accessTokenFactory: () => localStorage.getItem('psiagenda_token') })
      .withAutomaticReconnect()
      .build()

    conexao.on('ParticipanteEntrou', iniciarOferta)
    conexao.on('SinalRecebido', tratarSinal)
    conexao.on('ParticipanteSaiu', () => {
      estado.value = 'A outra pessoa saiu.'
      conectado.value = false
      if (videoRemoto.value) videoRemoto.value.srcObject = null
    })

    await conexao.start()
    await conexao.invoke('EntrarNaSala', sala.value.salaVideoId)
    estado.value = 'Aguardando a outra pessoa...'
  } catch (e) {
    if (e?.name === 'NotAllowedError') {
      erro.value = 'Permissao de camera/microfone negada. Libere o acesso no navegador e recarregue.'
    } else if (e?.name === 'NotFoundError') {
      erro.value = 'Nenhuma camera ou microfone encontrado neste dispositivo.'
    } else {
      erro.value = mensagemDeErro(e, 'Nao foi possivel entrar na sala.')
    }
    estado.value = 'Erro'
  }
}

function alternarMicrofone() {
  microfoneLigado.value = !microfoneLigado.value
  midiaLocal?.getAudioTracks().forEach((t) => (t.enabled = microfoneLigado.value))
}

function alternarCamera() {
  cameraLigada.value = !cameraLigada.value
  midiaLocal?.getVideoTracks().forEach((t) => (t.enabled = cameraLigada.value))
}

async function encerrar() {
  await limpar()
  router.back()
}

async function limpar() {
  try {
    if (conexao && sala.value) await conexao.invoke('SairDaSala', sala.value.salaVideoId)
  } catch {
    // A conexao pode ja ter caido; sair mesmo assim.
  }
  // Soltar a camera e obrigatorio: sem isso o LED fica aceso apos sair da tela.
  midiaLocal?.getTracks().forEach((t) => t.stop())
  peer?.close()
  await conexao?.stop()
  peer = null
  conexao = null
  midiaLocal = null
}

onMounted(entrar)
onBeforeUnmount(limpar)
</script>

<template>
  <div class="sala">
    <div class="sala-topo">
      <div>
        <strong>Consulta online</strong>
        <span v-if="sala" class="horario">
          {{ formatarHora(sala.inicioUtc) }} as {{ formatarHora(sala.fimUtc) }}
        </span>
      </div>
      <span class="estado">{{ estado }}</span>
    </div>

    <Message v-if="erro" severity="error" :closable="false">{{ erro }}</Message>

    <div class="videos">
      <div class="video-remoto">
        <video ref="videoRemoto" autoplay playsinline />
        <div v-if="!conectado" class="aguardando">
          <i class="pi pi-user" style="font-size: 2.5rem" />
          <p>{{ estado }}</p>
        </div>
      </div>

      <video ref="videoLocal" class="video-local" autoplay playsinline muted />
    </div>

    <div class="controles">
      <Button :icon="microfoneLigado ? 'pi pi-microphone' : 'pi pi-microphone-slash'"
              :severity="microfoneLigado ? 'secondary' : 'danger'" rounded @click="alternarMicrofone"
              :aria-label="microfoneLigado ? 'Desligar microfone' : 'Ligar microfone'" />
      <Button :icon="cameraLigada ? 'pi pi-video' : 'pi pi-eye-slash'"
              :severity="cameraLigada ? 'secondary' : 'danger'" rounded @click="alternarCamera"
              :aria-label="cameraLigada ? 'Desligar camera' : 'Ligar camera'" />
      <Button icon="pi pi-phone" severity="danger" rounded @click="encerrar" aria-label="Encerrar" />
    </div>
  </div>
</template>

<style scoped>
.sala {
  min-height: 100vh;
  background: #0f172a;
  color: #fff;
  display: flex;
  flex-direction: column;
  padding: 1rem;
  gap: 1rem;
}

.sala-topo {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.horario {
  margin-left: 0.75rem;
  color: #94a3b8;
  font-size: 0.9rem;
}

.estado {
  color: #93c5fd;
  font-size: 0.85rem;
}

.videos {
  flex: 1;
  position: relative;
  border-radius: 12px;
  overflow: hidden;
  background: #1e293b;
  min-height: 400px;
}

.video-remoto,
.video-remoto video {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.aguardando {
  position: absolute;
  inset: 0;
  display: grid;
  place-content: center;
  justify-items: center;
  gap: 0.5rem;
  color: #64748b;
}

.video-local {
  position: absolute;
  right: 1rem;
  bottom: 1rem;
  width: 180px;
  border-radius: 10px;
  border: 2px solid #334155;
  background: #000;
  transform: scaleX(-1);
}

.controles {
  display: flex;
  justify-content: center;
  gap: 1rem;
}
</style>
