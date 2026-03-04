"use strict";
var _a, _b, _c, _d, _e, _f;
const POSITION_ORDER = {
    'G': 0, 'G-F': 1, 'F-G': 2,
    'F': 3, 'F-C': 4, 'C-F': 5,
    'C': 6, '': 99
};
// =============================================
// 現在のソート状態
// =============================================
let currentSortKey = 'name';
let currentSortDir = 'asc';
// =============================================
// ソート値取得
// =============================================
function getSortValue(el, key) {
    var _a, _b;
    const v = (_a = el.dataset[key]) !== null && _a !== void 0 ? _a : '';
    switch (key) {
        case 'jersey':
        case 'weight':
            return parseFloat(v) || 0;
        case 'height': {
            // "6-9" → インチ換算で数値比較
            const parts = v.split('-');
            return parts.length === 2
                ? parseInt(parts[0]) * 12 + parseInt(parts[1])
                : 0;
        }
        case 'position':
            return (_b = POSITION_ORDER[v]) !== null && _b !== void 0 ? _b : 99;
        default:
            return v.toLowerCase();
    }
}
// =============================================
// DOM並び替え実行
// =============================================
function sortPlayers(key, dir) {
    // テーブル行
    const tbody = document.getElementById('playersTableBody');
    if (tbody) {
        const rows = Array.from(tbody.querySelectorAll('tr.player-row'));
        rows.sort((a, b) => {
            const av = getSortValue(a, key);
            const bv = getSortValue(b, key);
            if (av < bv)
                return dir === 'asc' ? -1 : 1;
            if (av > bv)
                return dir === 'asc' ? 1 : -1;
            return 0;
        });
        rows.forEach(row => tbody.appendChild(row));
    }
    // カード
    const cardGrid = document.getElementById('playersCardGrid');
    if (cardGrid) {
        const cols = Array.from(cardGrid.querySelectorAll('.player-card-col'));
        cols.sort((a, b) => {
            const av = getSortValue(a, key);
            const bv = getSortValue(b, key);
            if (av < bv)
                return dir === 'asc' ? -1 : 1;
            if (av > bv)
                return dir === 'asc' ? 1 : -1;
            return 0;
        });
        cols.forEach(col => cardGrid.appendChild(col));
    }
    // ヘッダーアイコン更新
    document.querySelectorAll('th.sortable').forEach(th => {
        const icon = th.querySelector('.sort-icon');
        if (!icon)
            return;
        if (th.dataset.col === key) {
            icon.className = `bi bi-chevron-${dir === 'asc' ? 'up' : 'down'} sort-icon sort-active`;
        }
        else {
            icon.className = 'bi bi-chevron-expand sort-icon';
        }
    });
    currentSortKey = key;
    currentSortDir = dir;
}
// =============================================
// ドロップダウン選択
// =============================================
document.querySelectorAll('.sort-item').forEach(item => {
    item.addEventListener('click', e => {
        var _a, _b;
        e.preventDefault();
        const key = item.dataset.sort;
        const dir = item.dataset.dir;
        // active 切替
        document.querySelectorAll('.sort-item').forEach(i => i.classList.remove('active'));
        item.classList.add('active');
        // ラベル更新
        const label = document.getElementById('sortLabel');
        if (label)
            label.textContent = (_b = (_a = item.textContent) === null || _a === void 0 ? void 0 : _a.trim()) !== null && _b !== void 0 ? _b : '並び替え';
        sortPlayers(key, dir);
        applyFilter();
    });
});
// =============================================
// テーブルヘッダークリックソート
// =============================================
document.querySelectorAll('th.sortable').forEach(th => {
    th.style.cursor = 'pointer';
    th.addEventListener('click', () => {
        const key = th.dataset.col;
        const dir = (currentSortKey === key && currentSortDir === 'asc') ? 'desc' : 'asc';
        sortPlayers(key, dir);
        applyFilter();
    });
});
// =============================================
// 検索フィルター
// =============================================
function applyFilter() {
    var _a, _b;
    const q = (_b = (_a = document.getElementById('searchInput')) === null || _a === void 0 ? void 0 : _a.value.trim().toLowerCase()) !== null && _b !== void 0 ? _b : '';
    document.querySelectorAll('tr.player-row').forEach(row => {
        var _a, _b, _c;
        const hit = !q
            || ((_a = row.dataset.name) !== null && _a !== void 0 ? _a : '').toLowerCase().includes(q)
            || ((_b = row.dataset.position) !== null && _b !== void 0 ? _b : '').toLowerCase().includes(q)
            || ((_c = row.dataset.country) !== null && _c !== void 0 ? _c : '').toLowerCase().includes(q);
        row.style.display = hit ? '' : 'none';
    });
    document.querySelectorAll('.player-card-col').forEach(col => {
        var _a, _b, _c;
        const hit = !q
            || ((_a = col.dataset.name) !== null && _a !== void 0 ? _a : '').toLowerCase().includes(q)
            || ((_b = col.dataset.position) !== null && _b !== void 0 ? _b : '').toLowerCase().includes(q)
            || ((_c = col.dataset.country) !== null && _c !== void 0 ? _c : '').toLowerCase().includes(q);
        col.style.display = hit ? '' : 'none';
    });
}
(_a = document.getElementById('searchBtn')) === null || _a === void 0 ? void 0 : _a.addEventListener('click', applyFilter);
(_b = document.getElementById('searchInput')) === null || _b === void 0 ? void 0 : _b.addEventListener('keydown', (e) => {
    if (e.key === 'Enter')
        applyFilter();
});
(_c = document.getElementById('clearBtn')) === null || _c === void 0 ? void 0 : _c.addEventListener('click', () => {
    document.getElementById('searchInput').value = '';
    applyFilter();
    document.getElementById('clearBtn').style.display = 'none';
});
(_d = document.getElementById('searchInput')) === null || _d === void 0 ? void 0 : _d.addEventListener('input', () => {
    const val = document.getElementById('searchInput').value;
    const clearBtn = document.getElementById('clearBtn');
    if (clearBtn)
        clearBtn.style.display = val ? 'inline-block' : 'none';
});
// =============================================
// ビュー切替
// =============================================
function setView(mode) {
    const tableView = document.getElementById('tableView');
    const cardView = document.getElementById('cardView');
    const btnTable = document.getElementById('btnTable');
    const btnCard = document.getElementById('btnCard');
    if (tableView)
        tableView.style.display = mode === 'table' ? 'block' : 'none';
    if (cardView)
        cardView.style.display = mode === 'card' ? 'block' : 'none';
    btnTable === null || btnTable === void 0 ? void 0 : btnTable.classList.toggle('active', mode === 'table');
    btnCard === null || btnCard === void 0 ? void 0 : btnCard.classList.toggle('active', mode === 'card');
    localStorage.setItem('nbaViewMode', mode);
}
(_e = document.getElementById('btnTable')) === null || _e === void 0 ? void 0 : _e.addEventListener('click', () => setView('table'));
(_f = document.getElementById('btnCard')) === null || _f === void 0 ? void 0 : _f.addEventListener('click', () => setView('card'));
const savedMode = localStorage.getItem('nbaViewMode');
if (savedMode)
    setView(savedMode);
// =============================================
// 初期ソート（選手名 A→Z）
// =============================================
sortPlayers('position', 'asc');
