document.addEventListener('DOMContentLoaded', () => {
  buildHouses();
  buildGates();
  buildSwirls();
  buildRunes();
  buildGhosts();
  buildWards();
  assignPathVariants();
  checkTileImages();
  buildCemeteryDecor();
  // buildAssetShowcase();
  buildCharacterTest();
});

/* ---------- reusable component builders (visual only, no game logic) ---------- */

function createHouse(index){
  const el = document.createElement('div');
  el.className = 'house';
  el.innerHTML = `
    <img class="house-photo" src="assets/houses/house-${index}.png" alt=""
         onload="this.closest('.house').classList.add('house--has-image');"
         onerror="this.remove();">
    <div class="house-glow"></div>
    <div class="house-smoke"></div>
    <div class="house-chimney"></div>
    <div class="house-roof"></div>
    <div class="house-body">
      <span class="house-window"></span>
      <span class="house-door"></span>
      <span class="house-window"></span>
    </div>
    <div class="house-sign"><span>TRICK · OR · TREAT</span></div>
  `;
  return el;
}

function buildHouses(){
  document.querySelectorAll('[data-role="house"]').forEach((slot, i) => slot.appendChild(createHouse(i + 1)));
}

function assignPathVariants(){
  document.querySelectorAll('.tile--path').forEach((tile, i) => {
    tile.classList.add('tile--path-' + ((i % 3) + 1));
  });
}

function createGate(){
  const el = document.createElement('div');
  el.className = 'gate';
  el.innerHTML = `
    <div class="gate-post left"></div>
    <div class="gate-arch"><span class="gate-finial"></span></div>
    <div class="gate-post right"></div>
    <div class="gate-opening"></div>
  `;
  return el;
}

function spiralPath(cx, cy, turns, rMax, steps){
  let d = '';
  for (let i = 0; i <= steps; i++){
    const t = i / steps;
    const angle = t * turns * Math.PI * 2;
    const r = t * rMax;
    const x = cx + r * Math.cos(angle);
    const y = cy + r * Math.sin(angle);
    d += (i === 0 ? 'M' : 'L') + x.toFixed(2) + ',' + y.toFixed(2) + ' ';
  }
  return d.trim();
}

function createSwirl(){
  const size = 60;
  const d = spiralPath(size / 2, size / 2, 2.4, size / 2 - 5, 40);
  const wrap = document.createElement('div');
  wrap.className = 'swirl-icon';
  wrap.innerHTML = `<svg viewBox="0 0 ${size} ${size}">
    <path d="${d}" fill="none" stroke="currentColor" stroke-width="3.2" stroke-linecap="round"/>
  </svg>`;
  return wrap;
}

function createRune(){
  const wrap = document.createElement('div');
  wrap.className = 'rune-icon';
  wrap.innerHTML = `<svg viewBox="0 0 60 60">
    <circle cx="30" cy="30" r="19" fill="none" stroke="currentColor" stroke-width="2.4" opacity=".7"/>
    <path d="M30 11 L39 30 L30 49 L21 30 Z" fill="currentColor"/>
    <circle cx="30" cy="30" r="3.5" fill="#b4520f"/>
  </svg>`;
  return wrap;
}

function createGhost(){
  const wrap = document.createElement('div');
  wrap.className = 'ghost-icon';
  wrap.innerHTML = `<svg viewBox="0 0 60 70">
    <path d="M10 42 C10 16 19 6 30 6 C41 6 50 16 50 42 L50 63 L42 55 L34 63 L26 55 L18 63 L10 55 Z" fill="currentColor"/>
    <circle cx="22" cy="35" r="3.4" fill="#1a1420"/>
    <circle cx="38" cy="35" r="3.4" fill="#1a1420"/>
  </svg>`;
  return wrap;
}

function createWardIcon(){
  const wrap = document.createElement('div');
  wrap.className = 'ward-icon';
  wrap.innerHTML = `<svg viewBox="0 0 60 70">
    <path d="M10 42 C10 16 19 6 30 6 C41 6 50 16 50 42 L50 63 L42 55 L34 63 L26 55 L18 63 L10 55 Z" fill="currentColor" opacity=".55"/>
    <line x1="12" y1="14" x2="48" y2="56" stroke="#e23b3b" stroke-width="5" stroke-linecap="round"/>
    <line x1="48" y1="14" x2="12" y2="56" stroke="#e23b3b" stroke-width="5" stroke-linecap="round"/>
  </svg>`;
  return wrap;
}


function buildGates(){
  document.querySelectorAll('[data-role="gate"]').forEach(slot => slot.appendChild(createGate()));
}
function buildSwirls(){
  document.querySelectorAll('.tile--green').forEach(tile => tile.appendChild(createSwirl()));
}
function buildRunes(){
  document.querySelectorAll('.tile--orange').forEach(tile => tile.appendChild(createRune()));
}
function buildGhosts(){
  document.querySelectorAll('.tile--ghost').forEach(tile => tile.appendChild(createGhost()));
}
function buildWards(){
  document.querySelectorAll('.tile--warded').forEach(tile => tile.appendChild(createWardIcon()));
}

function buildCemeteryDecor(){
  const ground = document.querySelector('.cemetery-ground');
  if (!ground) return;
  const layer = document.createElement('div');
  layer.className = 'cemetery-deco';
  const items = [
    { type: 'tomb', x: 6,  y: 18, r: -7 },
    { type: 'tomb', x: 20, y: 58, r: 5 },
    { type: 'tomb', x: 38, y: 12, r: -3 },
    { type: 'tomb', x: 57, y: 62, r: 6 },
    { type: 'tomb', x: 74, y: 22, r: -5 },
    { type: 'tomb', x: 90, y: 55, r: 4 },
    { type: 'tree',    x: 12, y: 78 },
    { type: 'tree',    x: 86, y: 80 },
    { type: 'pumpkin', x: 48, y: 84 },
    { type: 'pumpkin', x: 66, y: 15 },
  ];
  items.forEach(it => {
    const el = document.createElement('div');
    el.className = 'deco deco--' + it.type;
    el.style.left = it.x + '%';
    el.style.top = it.y + '%';
    if (it.r) el.style.transform = 'rotate(' + it.r + 'deg)';
    layer.appendChild(el);
  });
  ground.appendChild(layer);
}
function markIfImageExists(url, selector){
  const img = new Image();
  img.onload = () => document.querySelectorAll(selector).forEach(el => el.classList.add('has-photo'));
  img.src = url;
}

function checkTileImages(){
  markIfImageExists('assets/tiles/orange.png', '.tile--orange');
  markIfImageExists('assets/tiles/purple.png', '.tile--purple');
  markIfImageExists('assets/tiles/green.png', '.tile--green');
  markIfImageExists('assets/tiles/ghost.png', '.tile--ghost');
  markIfImageExists('assets/tiles/coffin.png', '.tile--coffin');
  markIfImageExists('assets/tiles/path-1.png', '.tile--path-1');
  markIfImageExists('assets/tiles/path-2.png', '.tile--path-2');
  markIfImageExists('assets/tiles/path-3.png', '.tile--path-3');
}
// const ASSET_CATEGORIES = [
//   { key:'char',    label:'کاراکتر',      folder:'assets/characters', prefix:'char',      count:5, scales:[2, 1.8, 1.8, 2, 1.9] },
//   { key:'candy',   label:'کندی',         folder:'assets/candy',      prefix:'candy',     count:9, scales:1 },
//   { key:'glow',    label:'گلو استیک',    folder:'assets/tokens',     prefix:'glowstick', count:1, scales:1.5 },
//   { key:'banshee', label:'بنشی',         folder:'assets/tokens',     prefix:'banshee',   count:1, scales:2 },
//   { key:'ghost',   label:'روح',          folder:'assets/tokens',     prefix:'ghost',     count:3, scales:2 },
//   { key:'cq',      label:'کندی کوئست',   folder:'assets/candyquest', prefix:'cq',        count:9, scales:1, hasBg:true },
//   { key:'boost',   label:'توکن BOOst',   folder:'assets/boost',      prefix:'boost',     count:12, scales:1.5 },
// ];


// function buildAssetShowcase(){
//   const wrap = document.createElement('div');
//   wrap.className = 'asset-showcase';

//   ASSET_CATEGORIES.forEach(cat => {
//     const row = document.createElement('div');
//     row.className = 'asset-row';
//     const title = document.createElement('div');
//     title.className = 'asset-row-title';
//     title.textContent = cat.label;
//     row.appendChild(title);

//     const itemsWrap = document.createElement('div');
//     itemsWrap.className = 'asset-items';
// for (let i = 1; i <= cat.count; i++){
  
//   const fname = cat.count === 1 ? `${cat.prefix}.png` : `${cat.prefix}-${i}.png`;
//   const scale = Array.isArray(cat.scales) ? cat.scales[i - 1] : cat.scales;
//   const item = document.createElement('div');
//   item.className = 'asset-item' + (cat.hasBg ? ' asset-item--cq' : '');
//   item.dataset.id = `${cat.key}-${i}`;
// if (cat.hasBg){
//   item.style.setProperty('--cq-img', `url('${cat.folder}/${fname}')`);
// } else {
//   item.style.backgroundImage = `url('${cat.folder}/${fname}')`;
// }
//   item.style.setProperty('--scale', scale);
//   item.innerHTML = `<span class="asset-item-label">${cat.count === 1 ? cat.label : i}</span>`;
//   itemsWrap.appendChild(item);
  
// }
//     row.appendChild(itemsWrap);
//     wrap.appendChild(row);
//   });

//   const boardWrapper = document.querySelector('.board-wrapper');
//   (boardWrapper || document.body).appendChild(wrap);
// }



function createStarPath(cx, cy, points, outerR, innerR){
  let d = '';
  const step = Math.PI / points;
  for (let i = 0; i < points * 2; i++){
    const r = i % 2 === 0 ? outerR : innerR;
    const angle = i * step - Math.PI / 2;
    const x = cx + r * Math.cos(angle);
    const y = cy + r * Math.sin(angle);
    d += (i === 0 ? 'M' : 'L') + x.toFixed(2) + ',' + y.toFixed(2) + ' ';
  }
  return d + 'Z';
}

function createCandySVG(){
  return `
    <g>
      <!-- کاغذ پیچیده سمت چپ -->
      <path d="M18 30 L2 16 L2 22 L10 30 L2 38 L2 44 Z" fill="currentColor"/>
      <line x1="6" y1="20" x2="6" y2="40" stroke="currentColor" stroke-width="1.4" opacity=".55"/>
      <line x1="10" y1="17" x2="10" y2="43" stroke="currentColor" stroke-width="1.4" opacity=".4"/>

      <!-- کاغذ پیچیده سمت راست -->
      <path d="M42 30 L58 16 L58 22 L50 30 L58 38 L58 44 Z" fill="currentColor"/>
      <line x1="54" y1="20" x2="54" y2="40" stroke="currentColor" stroke-width="1.4" opacity=".55"/>
      <line x1="50" y1="17" x2="50" y2="43" stroke="currentColor" stroke-width="1.4" opacity=".4"/>

      <!-- بدنه اصلی آبنبات -->
      <rect x="16" y="19" width="28" height="22" rx="11" fill="currentColor"/>

      <!-- خط‌های چین کاغذ روی بدنه -->
      <line x1="21" y1="21" x2="21" y2="39" stroke="#000" stroke-opacity=".15" stroke-width="1.6"/>
      <line x1="39" y1="21" x2="39" y2="39" stroke="#000" stroke-opacity=".15" stroke-width="1.6"/>

      <!-- هایلایت براق -->
      <ellipse cx="24" cy="24" rx="4.5" ry="2.6" fill="#fff" opacity=".35"/>
    </g>
  `;
}

function createGhostDieFace(number, plain){
  const wrap = document.createElement('div');
  wrap.className = 'dice-face dice-face--ghost';
  if (plain){
    wrap.innerHTML = `<span class="dice-plain-number">${number}</span>`;
  } else {
    wrap.innerHTML = `<svg viewBox="0 0 60 70">
      <path d="M10 42 C10 16 19 6 30 6 C41 6 50 16 50 42 L50 63 L42 55 L34 63 L26 55 L18 63 L10 55 Z" fill="#ffffff"/>
      <text x="30" y="40" text-anchor="middle" font-size="26" font-family="'Creepster',cursive" fill="#123a30">${number}</text>
    </svg>`;
  }
  return wrap;
}

function createPlayerDieFace(number){
  const wrap = document.createElement('div');
  wrap.className = 'dice-face dice-face--player';
  const starD = createStarPath(30, 30, 15, 26, 14);
  wrap.innerHTML = `<svg viewBox="0 0 60 60">
    <path d="${starD}" fill="#000000"/>
    <text x="30" y="38" text-anchor="middle" font-size="22" font-family="'Creepster',cursive" fill="#ffffff">${number}</text>
  </svg>`;
  return wrap;
}

function createHHDieFace(type){
  const wrap = document.createElement('div');
  wrap.className = 'dice-face dice-face--hh';
  if (type === 'boo') wrap.innerHTML = `<span class="dice-hh-text">BOOOOO</span>`;
  else if (type === 'boost') wrap.innerHTML = `<span class="dice-hh-text">BOOOOST</span>`;
  else if (type === 'again') wrap.innerHTML = `
    <svg viewBox="0 0 60 60" class="dice-hh-candy">${createCandySVG()}</svg>
    <span class="dice-hh-sub">+ دوباره بنداز</span>`;
  else if (type === 'x2') wrap.innerHTML = `
    <div class="dice-hh-row">
      <span class="dice-hh-x">×2</span>
      <svg viewBox="0 0 60 60" class="dice-hh-candy">${createCandySVG()}</svg>
    </div>`;
  else if (type === 'x3') wrap.innerHTML = `
    <div class="dice-hh-row">
      <span class="dice-hh-x">×3</span>
      <svg viewBox="0 0 60 60" class="dice-hh-candy">${createCandySVG()}</svg>
    </div>`;
  else if (type === 'swirl'){
    const d = spiralPath(30, 30, 2.4, 25, 40);
    wrap.innerHTML = `<svg viewBox="0 0 60 60"><path d="${d}" fill="none" stroke="currentColor" stroke-width="4" stroke-linecap="round"/></svg>`;
  }
  return wrap;
}

const CHARACTER_SCALES = [2, 1.8, 1.8, 2, 1.9];

function createCharacterToken(index, scale){
  const el = document.createElement('div');
  el.className = 'char-token';
  el.style.setProperty('--scale', scale);
el.style.backgroundImage = `url('assets/character-tokens/token-${index}.png')`;
  return el;
}

function buildCharacterTest(){
  const root = document.getElementById('characterTestRoot');
  if (!root) return;

  const wrap = document.createElement('div');
  wrap.className = 'char-test';
  wrap.innerHTML = `<div class="char-test-title">کاراکتر توکن‌ها</div>`;

  const row = document.createElement('div');
  row.className = 'char-test-row';
  CHARACTER_SCALES.forEach((scale, i) => row.appendChild(createCharacterToken(i + 1, scale)));

  wrap.appendChild(row);
  root.appendChild(wrap);
}