// render.js — game state -> DOM only. No network calls.
const Render = (() => {
  // backend house-N -> [col,row] of its .house-slot in index.html
  const HOUSE_GRID = {
    'house-1': [13, 5], 'house-2': [13, 9], 'house-3': [8, 13], 'house-4': [11, 13],
    'house-5': [4, 13], 'house-6': [1, 13], 'house-7': [1, 10], 'house-8': [1, 8],
    'house-9': [1, 5], 'house-10': [3, 1], 'house-11': [5, 1], 'house-12': [9, 1],
  };
  // Cemetery tiles are positioned by % in index.html. Key = 'left|top'; first alias = primary id.
  // Backend currently has duplicate nodes for the same physical tile -> aliases share one element.
  const GY = {
    '12|50': ['cem-C7'], '25|50': ['cem-C6'],
    '38|50': ['grave-safe-W', 'cem-C5'],
    '50|50': ['grave-center', 'cem-C4'],
    '62|50': ['grave-safe-E', 'cem-C3'],
    '75|50': ['cem-C2'], '88|50': ['cem-C1'],
    '50|38': ['grave-safe-N', 'path-6-6'], '50|26': ['path-6-5'], '50|14': ['path-6-4'], '50|4': ['gate-6-3'],
    '50|62': ['grave-safe-S', 'path-6-8'], '50|74': ['path-6-9'], '50|86': ['path-6-10'], '50|96': ['gate-6-11'],
    '14|82': ['ghost-start'], '23|72': ['candy-coffin'],
    '32|69': ['ghost-diag-2'], '41|65': ['ghost-diag-1'],
  };
  const NO_DOM = /^conn-|^gate-10-7$/; // graph nodes with no tile in index.html

  const els = {};          // backend tile id -> element
  const unmapped = [];
  let built = false;
  let highlighted = [];

  const $ = (id) => document.getElementById(id);
  const h = (tag, cls, text) => {
    const n = document.createElement(tag);
    if (cls) n.className = cls;
    if (text != null) n.textContent = text;
    return n;
  };
  const num = (s, re) => { const m = s.match(re); return m ? parseFloat(m[1]) : null; };

  function coordsFor(id) {
    if (id === 'start-A') return [12, 2];
    if (id === 'start-B') return [12, 12];
    if (HOUSE_GRID[id]) return HOUSE_GRID[id];
    const m = id.match(/-(\d+)-(\d+)$/);
    if (!m) return null;
    const col = +m[1], row = +m[2];
    return [col >= 15 ? col - 3 : col, row]; // backend right column (15) = HTML column 12
  }

  function extra(board, cls, text, pos) {
    const d = h('div', `hh-extra ${cls}`, text);
    if (pos) { d.style.gridColumn = String(pos[0]); d.style.gridRow = String(pos[1]); }
    board.appendChild(d);
    return d;
  }

  function build(ids) {
    const board = $('game-board');
    const grid = new Map();
    board.querySelectorAll(':scope > .tile, :scope > .house-slot').forEach((el) => {
      const st = el.getAttribute('style') || '';
      const c = num(st, /grid-column:\s*(\d+)/), r = num(st, /grid-row:\s*(\d+)/);
      if (c && r) grid.set(`${c},${r}`, el);
    });
    const portal1 = extra(board, 'hh-portal', '🌀', [1, 1]);
    const portal2 = extra(board, 'hh-portal', '🌀', [13, 13]);
    const bansheeStart = extra(board, 'hh-banshee-start', '🕯️', null);

    const gy = new Map();
    document.querySelectorAll('.graveyard-layer > .tile').forEach((el) => {
      const st = el.getAttribute('style') || '';
      const l = num(st, /left:\s*([\d.]+)%/), t = num(st, /top:\s*([\d.]+)%/);
      if (l != null && t != null) gy.set(`${Math.round(l)}|${Math.round(t)}`, el);
    });

    const idSet = new Set(ids);
    const gyIds = new Set();
    for (const [key, aliases] of Object.entries(GY)) {
      aliases.forEach((a) => gyIds.add(a));
      const el = gy.get(key);
      if (!el) continue;
      el.dataset.tileId = aliases[0];
      aliases.forEach((a) => { if (idSet.has(a)) els[a] = el; });
    }

    for (const id of ids) {
      if (els[id]) continue;
      let el = null;
      if (id === 'banshee-start') el = bansheeStart;
      else if (id === 'portal-1') el = portal1;
      else if (id === 'portal-2') el = portal2;
      else if (!NO_DOM.test(id) && !gyIds.has(id)) {
        const rc = coordsFor(id);
        if (rc) el = grid.get(rc.join(',')) || null;
      }
      if (el) { els[id] = el; if (!el.dataset.tileId) el.dataset.tileId = id; }
      else unmapped.push(id);
    }
    if (unmapped.length) console.warn('[Render] tiles without DOM element:', unmapped);
    built = true;
  }
  function layer(el, cls = 'hh-pieces') {
    let l = el.querySelector(`:scope > .${cls}`);
    if (!l) { l = h('div', cls); el.appendChild(l); }
    return l;
  }


  function ghostNode(id) {
    const n = h('div', 'hh-piece hh-ghost');
    n.title = id;
    const img = new Image();
    img.alt = id;
    img.src = id === 'Banshee' ? 'assets/tokens/banshee.png' : `assets/tokens/ghost-${id.slice(-1)}.png`;
    img.onerror = () => { img.remove(); n.textContent = id === 'Banshee' ? '👹' : '👻'; };
    n.appendChild(img);
    return n;
  }

  function pieces(s) {
    document.querySelectorAll('.hh-pieces, .hh-marks').forEach((l) => l.replaceChildren());
    const lost = [];
    const put = (tileId, node, label) => {
      const el = tileId ? els[tileId] : null;
      if (el) layer(el).appendChild(node); else lost.push(`${label}@${tileId ?? '—'}`);
    };
    s.players.forEach((p, i) => {
      const idx = p.characterIndex || i + 1;

      // board piece = CHARACTER image
      const node = h('div', 'hh-piece hh-char');
      node.style.backgroundImage = `url('assets/characters/char-${idx}.png')`;
      node.style.setProperty('--scale', CHARACTER_SCALES[idx - 1] ?? 1.8);
      node.title = p.name;
      if (i === s.currentPlayerIndex) node.classList.add('hh-current');
      put(p.currentTileId, node, p.name);

      // visited-house marker = character TOKEN image, placed on each visited house
      p.visitedHouseNumbers.forEach((n) => {
        const houseEl = els[`house-${n}`];
        if (!houseEl) return;
        const mark = createCharacterToken(idx, 1);
        mark.classList.add('hh-mark');
        mark.title = `${p.name} visited`;
        layer(houseEl, 'hh-marks').appendChild(mark);
      });
    });
    s.ghosts.forEach((g) => { if (g.isActive) put(g.currentTileId, ghostNode(g.id), g.id); });
    if (s.banshee.isReleased) put(s.banshee.currentTileId, ghostNode('Banshee'), 'Banshee');
    return lost;
  }

  function signs(s) {
    Object.values(s.houses).forEach((hs) => {
      const el = els[`house-${hs.number}`];
      if (el) el.dataset.sign = hs.sign;
    });
  }

  function hud(s, ctx, lost) {
    $('hh-game').hidden = false;
    const cur = s.players[s.currentPlayerIndex];
    const mine = !!cur && cur.id === ctx.me;
    const you = s.players.find((p) => p.id === ctx.me);
    $('hh-status').textContent = `Turn ${s.turnNumber} · ${s.phase} · ${cur ? cur.name : '?'}${mine ? ' (you)' : ''}`;

    const box = $('hh-players');
    box.replaceChildren();
    s.players.forEach((p, i) => {
      const row = h('div', 'hh-player' + (i === s.currentPlayerIndex ? ' is-current' : ''));
      const candy = Object.values(p.candy).reduce((a, b) => a + b, 0);
      row.append(h('b', null, `${i === s.currentPlayerIndex ? '▶ ' : ''}${p.name}`));
      row.append(h('span', null, ` 🍬${candy} ✨${p.glowSticks} ⚡${p.boosts.length} 🏠${p.visitedHouseNumbers.length}`));
      if (p.id === ctx.me) row.append(h('div', 'hh-note', `Your secret quest: ${p.secretCandyQuest}`));
      box.append(row);
    });

    const coffin = Object.values(s.candyCoffin).reduce((a, b) => a + b, 0);
    const gh = s.ghosts.map((g) => `${g.id.replace('Ghost', 'G')}${g.isActive ? '✓' : '–'}`).join(' ');
    const ban = s.banshee.isReleased ? 'released' : `track ${s.banshee.trackPosition}/6`;
    $('hh-world').textContent = `${gh} · Banshee ${ban} · Coffin 🍬${coffin}` +
      (lost.length ? ` · off-board: ${lost.join(', ')}` : '');

    const m = you && /^house-(\d+)$/.exec(you.currentTileId);
    const canVisit = mine && !!m && !you.visitedHouseNumbers.includes(+m[1]);
    document.querySelector('#hh-hud [data-action="visit"]').disabled = !canVisit;
    document.querySelectorAll('#hh-hud [data-action="begin"], #hh-hud [data-action="end"], #hh-hud [data-action="reachable"]')
      .forEach((b) => { b.disabled = !mine; });
  }

  function render(s, ctx) {
    if (!built) build(Object.keys(s.tiles));
    const lost = pieces(s);
    signs(s);
    hud(s, ctx, lost);
  }

  function highlight(ids) {
    clearHighlight();
    const map = new Map();
    ids.forEach((id) => {
      const el = els[id];
      if (!el) return;
      if (!map.has(el)) map.set(el, []);
      map.get(el).push(id);
    });
    map.forEach((_, el) => el.classList.add('hh-reachable'));
    highlighted = [...map.keys()];
    return map;
  }
  function clearHighlight() {
    highlighted.forEach((el) => el.classList.remove('hh-reachable'));
    highlighted = [];
  }

  function log(lines) {
    const pre = $('hh-log');
    if (!pre || !lines.length) return;
    pre.append(lines.join('\n') + '\n');
    pre.textContent = pre.textContent.split('\n').slice(-150).join('\n');
    pre.scrollTop = pre.scrollHeight;
  }

  function addRow() {
    const rows = $('hh-rows');
    const i = rows.children.length;
    if (i >= 5) return;
    const r = h('div', 'hh-row hh-prow');
    const name = h('input');
    name.placeholder = `Player ${i + 1}`;
    const sel = h('select');
    ['start-A', 'start-B'].forEach((v) => sel.append(new Option(v, v)));
    sel.value = i % 2 ? 'start-B' : 'start-A';
    r.append(name, sel);
    rows.append(r);
  }

 function initHud() {
    const el = h('aside');
    el.id = 'hh-hud';
    el.innerHTML = `
      <header><b>Haunted Halloween</b><button data-action="toggle">–</button></header>
      <div id="hh-body">
        <section id="hh-game" hidden>
          <div id="hh-status"></div>
          <div id="hh-players"></div>
          <div id="hh-world"></div>
          <div class="hh-row">
            <button data-action="begin">Begin turn</button>
            <button data-action="end">End turn</button>
            <button data-action="bedtime">Bedtime</button>
            <button data-action="scores">Scores</button>
          </div>
          <div class="hh-row">
            <label>Move distance (temp) <input id="hh-dist" type="number" min="1" max="12" value="3"></label>
            <button data-action="reachable">Show moves</button>
          </div>
          <div class="hh-row"><button data-action="visit">Trick or Treat 🎃</button></div>
        </section>
        <pre id="hh-log"></pre>
      </div>`;
    document.body.appendChild(el);
  }

  return { initHud, render, highlight, clearHighlight, log };
})();
