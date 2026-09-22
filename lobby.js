// lobby.js — lobby screen (view only, no network). Handlers are supplied by main.js.
const LobbyView = (() => {
  const CHARACTERS = [1, 2, 3, 4, 5];
  const START_TILES = ['start-A', 'start-B'];
  let root, handlers, lastLobby = null, myId = null;

  const $ = (id) => document.getElementById(id);
  const el = (tag, cls, text) => {
    const n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  };
  const error = (msg) => { $('lb-error').textContent = msg || ''; };
  const run = (fn) => async () => { error(''); try { await fn(); } catch (e) { error(e.message); } };
  const isReady = () => !!lastLobby?.players.find((p) => p.id === myId)?.isReady;

  function init(h, prefillCode) {
    handlers = h;
    root = el('div');
    root.id = 'hh-lobby-screen';
    root.hidden = true;
    root.innerHTML = `
      <div class="lb-card">
        <h1>Haunted <span>Halloween</span></h1>
        <div id="lb-entry">
          <input id="lb-name" maxlength="20" placeholder="Your name">
          <button id="lb-create">Create room</button>
          <div class="lb-or">or join with a room code</div>
          <div class="lb-row"><input id="lb-code" maxlength="5" placeholder="CODE"><button id="lb-join">Join</button></div>
        </div>
        <div id="lb-room" hidden>
          <div class="lb-code-label">Room code</div>
          <div class="lb-code"><b id="lb-code-text"></b><button id="lb-copy">Copy link</button></div>
          <div id="lb-players"></div>
          <div id="lb-status"></div>
          <div class="lb-row"><button id="lb-ready"></button><button id="lb-leave">Leave</button></div>
        </div>
        <div id="lb-error"></div>
      </div>`;
    document.body.appendChild(root);
    $('lb-code').value = (prefillCode || '').toUpperCase();

    const name = () => $('lb-name').value.trim();
    $('lb-create').onclick = run(() => handlers.create(name()));
    $('lb-join').onclick = run(() => handlers.join($('lb-code').value.trim().toUpperCase(), name()));
    $('lb-ready').onclick = run(() => handlers.ready(!isReady()));
    $('lb-leave').onclick = run(() => handlers.leave());
    $('lb-copy').onclick = run(async () => {
      await navigator.clipboard.writeText(`${location.href.split('?')[0]}?room=${lastLobby.code}`);
      $('lb-status').textContent = 'Invite link copied.';
    });
  }

  function showEntry() {
    lastLobby = null; myId = null;
    root.hidden = false;
    $('lb-entry').hidden = false;
    $('lb-room').hidden = true;
  }

  function playerRow(lobby, p, isMe) {
    const row = el('div', 'lb-player' + (p.isReady ? ' is-ready' : ''));
    const face = el('div', 'lb-face');
    face.style.backgroundImage = `url('assets/characters/char-${p.character}.png')`;
    row.append(face, el('span', 'lb-pname', p.name + (isMe ? ' (you)' : '')));

    if (isMe && !p.isReady) {
      const picks = el('div', 'lb-picks');
      CHARACTERS.forEach((c) => {
        const b = el('button', 'lb-pick' + (c === p.character ? ' is-sel' : ''));
        b.style.backgroundImage = `url('assets/characters/char-${c}.png')`;
        b.title = `Character ${c}`;
        b.disabled = lobby.players.some((x) => x.id !== p.id && x.character === c);
        b.onclick = run(() => handlers.character(c));
        picks.append(b);
      });
      const sel = el('select');
      START_TILES.forEach((t) => sel.append(new Option(t, t)));
      sel.value = p.startTileId;
      sel.onchange = run(() => handlers.startTile(sel.value));
      row.append(picks, sel);
    } else {
      row.append(el('span', 'lb-start', p.startTileId));
    }
    row.append(el('span', 'lb-flag', p.isReady ? '✔ ready' : '…'));
    return row;
  }

  function showRoom(lobby, playerId) {
    lastLobby = lobby; myId = playerId;
    root.hidden = false;
    $('lb-entry').hidden = true;
    $('lb-room').hidden = false;
    $('lb-code-text').textContent = lobby.code;

    const box = $('lb-players');
    box.replaceChildren();
    lobby.players.forEach((p) => box.append(playerRow(lobby, p, p.id === playerId)));

    const ready = lobby.players.filter((p) => p.isReady).length;
    $('lb-status').textContent = lobby.players.length < 2
      ? 'Waiting for more players (min 2)…'
      : `${ready}/${lobby.players.length} ready — game starts when everyone is ready.`;
    const btn = $('lb-ready');
    btn.textContent = isReady() ? 'Not ready' : "I'm ready";
    btn.classList.toggle('is-on', isReady());
  }

  const hide = () => { if (root) root.hidden = true; };

  return { init, showEntry, showRoom, hide, error };
})();