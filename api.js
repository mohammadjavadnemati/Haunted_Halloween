// api.js — network layer only (REST + SignalR). No DOM access.
const API_BASE = window.HH_API_BASE || 'http://localhost:5300';

async function request(url, method, body) {
  const res = await fetch(url, {
    method,
    headers: body ? { 'Content-Type': 'application/json' } : {},
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let data = text;
  try { data = text ? JSON.parse(text) : null; } catch { /* plain-text error body */ }
  if (!res.ok) throw new Error(typeof data === 'string' && data ? data : `HTTP ${res.status}`);
  return data;
}

const game = (m, p, b) => request(`${API_BASE}/api/Game${p}`, m, b);
const lobby = (m, p, b) => request(`${API_BASE}/api/Lobby${p}`, m, b);
const qs = (o) => new URLSearchParams(o).toString();

const Api = {
  // ---- lobby ----
  lobbyCreate: (name) => lobby('POST', '/create', { name }),
  lobbyJoin: (code, name) => lobby('POST', `/${encodeURIComponent(code)}/join`, { name }),
  lobbyGet: (code) => lobby('GET', `/${code}`),
  lobbyCharacter: (code, playerId, character) => lobby('POST', `/${code}/character`, { playerId, character }),
  lobbyStartTile: (code, playerId, startTileId) => lobby('POST', `/${code}/start-tile`, { playerId, startTileId }),
  lobbyReady: (code, playerId, ready) => lobby('POST', `/${code}/ready`, { playerId, ready }),
  lobbyLeave: (code, playerId) => lobby('POST', `/${code}/leave`, { playerId }),

  // ---- game ----
  getGame: (id) => game('GET', `/${id}`),
  beginTurn: (id) => game('POST', `/${id}/begin-turn`),
  endTurn: (id) => game('POST', `/${id}/end-turn`),
  bedtime: (id, playerId) => game('POST', `/${id}/bedtime`, { playerId }),
  reachable: (id, playerId, maxDistance) =>
    game('GET', `/${id}/reachable-tiles?${qs({ playerId, maxDistance })}`),
  move: (id, playerId, destinationTileId, maxDistance) =>
    game('POST', `/${id}/move`, { playerId, destinationTileId, maxDistance }),
  activateGhost: (id, playerLandedTileId) =>
    game('POST', `/${id}/ghost/activate-check?${qs({ playerLandedTileId })}`),
  visitHouse: (id, playerId) => game('POST', `/${id}/house/visit?${qs({ playerId })}`),
  hauntedHouseVisit: (id, playerId) => game('POST', `/${id}/haunted-house/visit?${qs({ playerId })}`),
  scores: (id) => game('GET', `/${id}/scores`),

  // SignalR: "GameStateUpdated" (state), "GameLog" (string[]), "LobbyUpdated" (lobby)
  async connect(code, { onState, onLog, onLobby }) {
    if (typeof signalR === 'undefined') throw new Error('SignalR client script not loaded');
    const conn = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE}/hubs/game`, { withCredentials: false }) // CORS uses wildcard origin
      .withAutomaticReconnect()
      .build();
    conn.on('GameStateUpdated', onState);
    conn.on('GameLog', onLog);
    conn.on('LobbyUpdated', onLobby);
    conn.onreconnected(() => conn.invoke('JoinGame', code));
    await conn.start();
    await conn.invoke('JoinGame', code);
    return conn;
  },
};