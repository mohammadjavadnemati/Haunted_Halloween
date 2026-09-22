// main.js — session, lobby flow, game actions.
(() => {
  const SESSION_KEY = 'hh-session';
  let session = null;      // { code, playerId } — code doubles as gameId
  let state = null;
  let conn = null;
  let moveMap = new Map();
  let warnedEnums = false;

  const $ = (id) => document.getElementById(id);
  const log = (...lines) => Render.log(lines.flat());
  const me = () => session?.playerId;
  const isMyTurn = () => !!state && state.players[state.currentPlayerIndex]?.id === me();
  const dist = () => parseInt($('hh-dist').value, 10) || 1;

  const saveSession = (s) => { session = s; try { localStorage.setItem(SESSION_KEY, JSON.stringify(s)); } catch { /* ignore */ } };
  const loadSession = () => { try { return JSON.parse(localStorage.getItem(SESSION_KEY)); } catch { return null; } };
  const clearSession = () => { session = null; try { localStorage.removeItem(SESSION_KEY); } catch { /* ignore */ } };

  // ---------- state ----------
  function applyState(s) {
    if (typeof s.phase === 'number' && !warnedEnums) {
      warnedEnums = true;
      log('⚠ Server sends numeric enums. Add JsonStringEnumConverter in Program.cs.');
    }
    state = s;
    moveMap = new Map();
    Render.clearHighlight();
    LobbyView.hide();
    Render.render(s, { me: me(), gameId: s.gameId });
  }
  const refresh = async () => applyState(await Api.getGame(session.code));

  // ---------- lobby flow ----------
  async function onLobby(lobby) {
    if (state) return; // game already running
    if (lobby.started) return refresh();
    if (!lobby.players.some((p) => p.id === me())) { // removed / left
      clearSession();
      LobbyView.showEntry();
      return;
    }
    LobbyView.showRoom(lobby, me());
  }

  async function connect(code) {
    if (conn) { await conn.stop().catch(() => {}); conn = null; }
    try {
      conn = await Api.connect(code, {
        onState: applyState,
        onLog: (l) => log(l),
        onLobby: (l) => onLobby(l).catch((e) => LobbyView.error(e.message)),
      });
    } catch (e) {
      LobbyView.error(`Realtime connection failed: ${e.message}`);
    }
  }

  async function enter(r) {
    saveSession({ code: r.code, playerId: r.playerId });
    history.replaceState(null, '', `?room=${r.code}`);
    await connect(r.code);
    await onLobby(r.lobby);
  }

  const lobbyHandlers = {
    create: async (name) => enter(await Api.lobbyCreate(name)),
    join: async (code, name) => enter(await Api.lobbyJoin(code, name)),
    character: async (c) => onLobby(await Api.lobbyCharacter(session.code, me(), c)),
    startTile: async (t) => onLobby(await Api.lobbyStartTile(session.code, me(), t)),
    ready: async (v) => onLobby(await Api.lobbyReady(session.code, me(), v)),
    leave: async () => {
      const { code, playerId } = session;
      await Api.lobbyLeave(code, playerId);
      if (conn) { await conn.stop().catch(() => {}); conn = null; }
      clearSession();
      history.replaceState(null, '', location.pathname);
      LobbyView.showEntry();
    },
  };

  async function restore() {
    const room = new URLSearchParams(location.search).get('room') || '';
    LobbyView.init(lobbyHandlers, room);
    const saved = loadSession();
    if (saved && (!room || room.toUpperCase() === saved.code)) {
      try {
        session = saved;
        const lobby = await Api.lobbyGet(saved.code);
        if (!lobby.players.some((p) => p.id === saved.playerId)) throw new Error('not in room');
        await connect(saved.code);
        if (lobby.started) await refresh(); else LobbyView.showRoom(lobby, saved.playerId);
        return;
      } catch { clearSession(); }
    }
    LobbyView.showEntry();
  }

  // ---------- game actions ----------
  const actions = {
    toggle() { const b = $('hh-body'); b.hidden = !b.hidden; },
    async begin() { log((await Api.beginTurn(session.code)).log); await refresh(); },
    async end() { log((await Api.endTurn(session.code)).log); await refresh(); },
    async bedtime() {
      const r = await Api.bedtime(session.code, me());
      log(r.success ? r.log : `⚠ ${r.error}`);
      await refresh();
    },
    async scores() {
      const list = await Api.scores(session.code);
      log(list.map((x) => `${x.playerId.slice(0, 4)}: ${x.total} (candy ${x.candyPoints}, quest ${x.secretQuestBonusPoints}, HH ${x.hauntedHouseBonusPoints}, houses ${x.houseTokenPoints}, glow ${x.glowStickPoints})`));
    },
    async reachable() {
      if (!isMyTurn()) return log('Not your turn.');
      const list = await Api.reachable(session.code, me(), dist());
      moveMap = Render.highlight(list.map((r) => r.tileId));
      log(`${list.length} reachable tiles (distance ≤ ${dist()}).`);
    },
    async visit() {
      const p = state.players.find((x) => x.id === me());
      const r = p.currentTileId === 'house-10'
        ? await Api.hauntedHouseVisit(session.code, p.id)
        : await Api.visitHouse(session.code, p.id);
      if (!r.success) return log(`⚠ ${r.error}`);
      log(r.log);
      await refresh();
    },
  };

  const guard = (fn) => async (...a) => {
    try { await fn(...a); } catch (e) { log(`⚠ ${e.message}`); }
  };

  async function onBoardClick(e) {
    const tile = e.target.closest('.hh-reachable');
    if (!tile || !moveMap.has(tile)) return;
    const ids = moveMap.get(tile);
    const dest = ids.includes(tile.dataset.tileId) ? tile.dataset.tileId : ids[0];
    const r = await Api.move(session.code, me(), dest, dist());
    if (!r.success) return log(`⚠ ${r.error}`);
    log(`Moved to ${r.landedTileId}` +
      (r.grantsExtraRoll ? ' — extra roll' : '') +
      (r.grantsForcedMove2 ? ' — forced move 2' : ''));
    await Api.activateGhost(session.code, r.landedTileId); // backend requires this separate call
    await refresh();
  }

  function start() {
    Render.initHud();
    $('hh-hud').addEventListener('click', (e) => {
      const b = e.target.closest('[data-action]');
      if (b && actions[b.dataset.action]) guard(actions[b.dataset.action])();
    });
    $('game-board').addEventListener('click', guard(onBoardClick));
    restore().catch((e) => LobbyView.error(e.message));
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
  else start();
})();