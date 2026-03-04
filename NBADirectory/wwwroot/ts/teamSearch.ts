// =============================================
// 型定義
// =============================================
type SortKey = 'jersey' | 'name' | 'position' | 'height' | 'weight';
type SortDir = 'asc' | 'desc';

const POSITION_ORDER: Record<string, number> = {
    'G': 0, 'G-F': 1, 'F-G': 2,
    'F': 3, 'F-C': 4, 'C-F': 5,
    'C': 6, '': 99
};

// =============================================
// 現在のソート状態
// =============================================
let currentSortKey: SortKey = 'name';
let currentSortDir: SortDir = 'asc';

// =============================================
// ソート値取得
// =============================================
function getSortValue(el: HTMLElement, key: SortKey): string | number {
    const v = el.dataset[key] ?? '';
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
            return POSITION_ORDER[v] ?? 99;
        default:
            return v.toLowerCase();
    }
}

// =============================================
// DOM並び替え実行
// =============================================
function sortPlayers(key: SortKey, dir: SortDir): void {
    // テーブル行
    const tbody = document.getElementById('playersTableBody');
    if (tbody) {
        const rows = Array.from(tbody.querySelectorAll<HTMLElement>('tr.player-row'));
        rows.sort((a, b) => {
            const av = getSortValue(a, key);
            const bv = getSortValue(b, key);
            if (av < bv) return dir === 'asc' ? -1 : 1;
            if (av > bv) return dir === 'asc' ? 1 : -1;
            return 0;
        });
        rows.forEach(row => tbody.appendChild(row));
    }

    // カード
    const cardGrid = document.getElementById('playersCardGrid');
    if (cardGrid) {
        const cols = Array.from(cardGrid.querySelectorAll<HTMLElement>('.player-card-col'));
        cols.sort((a, b) => {
            const av = getSortValue(a, key);
            const bv = getSortValue(b, key);
            if (av < bv) return dir === 'asc' ? -1 : 1;
            if (av > bv) return dir === 'asc' ? 1 : -1;
            return 0;
        });
        cols.forEach(col => cardGrid.appendChild(col));
    }

    // ヘッダーアイコン更新
    document.querySelectorAll<HTMLElement>('th.sortable').forEach(th => {
        const icon = th.querySelector<HTMLElement>('.sort-icon');
        if (!icon) return;
        if (th.dataset.col === key) {
            icon.className = `bi bi-chevron-${dir === 'asc' ? 'up' : 'down'} sort-icon sort-active`;
        } else {
            icon.className = 'bi bi-chevron-expand sort-icon';
        }
    });

    currentSortKey = key;
    currentSortDir = dir;
}

// =============================================
// ドロップダウン選択
// =============================================
document.querySelectorAll<HTMLElement>('.sort-item').forEach(item => {
    item.addEventListener('click', e => {
        e.preventDefault();
        const key = item.dataset.sort as SortKey;
        const dir = item.dataset.dir as SortDir;

        // active 切替
        document.querySelectorAll('.sort-item').forEach(i => i.classList.remove('active'));
        item.classList.add('active');

        // ラベル更新
        const label = document.getElementById('sortLabel');
        if (label) label.textContent = item.textContent?.trim() ?? '並び替え';

        sortPlayers(key, dir);
        applyFilter();
    });
});

// =============================================
// テーブルヘッダークリックソート
// =============================================
document.querySelectorAll<HTMLElement>('th.sortable').forEach(th => {
    th.style.cursor = 'pointer';
    th.addEventListener('click', () => {
        const key = th.dataset.col as SortKey;
        const dir = (currentSortKey === key && currentSortDir === 'asc') ? 'desc' : 'asc';
        sortPlayers(key, dir);
        applyFilter();
    });
});

// =============================================
// 検索フィルター
// =============================================
function applyFilter(): void {
    const q = (document.getElementById('searchInput') as HTMLInputElement)
        ?.value.trim().toLowerCase() ?? '';

    document.querySelectorAll<HTMLElement>('tr.player-row').forEach(row => {
        const hit = !q
            || (row.dataset.name ?? '').toLowerCase().includes(q)
            || (row.dataset.position ?? '').toLowerCase().includes(q)
            || (row.dataset.country ?? '').toLowerCase().includes(q);
        row.style.display = hit ? '' : 'none';
    });

    document.querySelectorAll<HTMLElement>('.player-card-col').forEach(col => {
        const hit = !q
            || (col.dataset.name ?? '').toLowerCase().includes(q)
            || (col.dataset.position ?? '').toLowerCase().includes(q)
            || (col.dataset.country ?? '').toLowerCase().includes(q);
        col.style.display = hit ? '' : 'none';
    });
}

document.getElementById('searchBtn')?.addEventListener('click', applyFilter);

document.getElementById('searchInput')?.addEventListener('keydown', (e: KeyboardEvent) => {
    if (e.key === 'Enter') applyFilter();
});

document.getElementById('clearBtn')?.addEventListener('click', () => {
    (document.getElementById('searchInput') as HTMLInputElement).value = '';
    applyFilter();
    document.getElementById('clearBtn')!.style.display = 'none';
});

document.getElementById('searchInput')?.addEventListener('input', () => {
    const val = (document.getElementById('searchInput') as HTMLInputElement).value;
    const clearBtn = document.getElementById('clearBtn');
    if (clearBtn) clearBtn.style.display = val ? 'inline-block' : 'none';
});

// =============================================
// ビュー切替
// =============================================
function setView(mode: 'table' | 'card'): void {
    const tableView = document.getElementById('tableView');
    const cardView = document.getElementById('cardView');
    const btnTable = document.getElementById('btnTable');
    const btnCard = document.getElementById('btnCard');
    if (tableView) tableView.style.display = mode === 'table' ? 'block' : 'none';
    if (cardView) cardView.style.display = mode === 'card' ? 'block' : 'none';
    btnTable?.classList.toggle('active', mode === 'table');
    btnCard?.classList.toggle('active', mode === 'card');
    localStorage.setItem('nbaViewMode', mode);
}

document.getElementById('btnTable')?.addEventListener('click', () => setView('table'));
document.getElementById('btnCard')?.addEventListener('click', () => setView('card'));

const savedMode = localStorage.getItem('nbaViewMode') as 'table' | 'card' | null;
if (savedMode) setView(savedMode);

// =============================================
// 初期ソート（選手名 A→Z）
// =============================================
sortPlayers('position', 'asc');