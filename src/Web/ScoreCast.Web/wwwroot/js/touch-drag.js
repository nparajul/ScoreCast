window.touchDrag = {
    init(containerId, dotnetRef) {
        const container = document.getElementById(containerId);
        if (!container) return;
        let dragIdx = -1, ghost = null, tiles = [], startY = 0, startX = 0, moved = false, holdTimer = null, held = false;

        container.addEventListener('touchstart', e => {
            const tile = e.target.closest('[data-drag-idx]');
            if (!tile) return;
            dragIdx = +tile.dataset.dragIdx;
            const t = e.touches[0];
            startX = t.clientX; startY = t.clientY; moved = false; held = false;
            tiles = [...container.querySelectorAll('[data-drag-idx]')];
            // Activate drag after 300ms hold
            holdTimer = setTimeout(() => {
                held = true;
                tile.style.transform = 'scale(1.05)';
                tile.style.boxShadow = '0 8px 24px rgba(0,0,0,0.3)';
                // Haptic feedback if available
                if (navigator.vibrate) navigator.vibrate(30);
            }, 300);
        }, { passive: true });

        container.addEventListener('touchmove', e => {
            if (dragIdx < 0) return;
            const t = e.touches[0];
            const dx = Math.abs(t.clientX - startX), dy = Math.abs(t.clientY - startY);

            // Cancel hold if moved before hold activates
            if (!held && (dx > 8 || dy > 8)) { clearTimeout(holdTimer); dragIdx = -1; return; }
            if (!held) return;

            moved = true;
            e.preventDefault();

            const tile = tiles[dragIdx];
            if (!ghost) {
                ghost = tile.cloneNode(true);
                ghost.style.cssText = `position:fixed;z-index:9999;pointer-events:none;opacity:0.85;transform:scale(1.05);width:${tile.offsetWidth}px;transition:none;box-shadow:0 8px 24px rgba(0,0,0,0.3);border-radius:16px;`;
                document.body.appendChild(ghost);
                tile.style.opacity = '0.3'; tile.style.transform = ''; tile.style.boxShadow = '';
            }
            ghost.style.left = (t.clientX - ghost.offsetWidth / 2) + 'px';
            ghost.style.top = (t.clientY - ghost.offsetHeight / 2) + 'px';

            for (const el of tiles) {
                const r = el.getBoundingClientRect();
                if (t.clientX >= r.left && t.clientX <= r.right && t.clientY >= r.top && t.clientY <= r.bottom) {
                    const targetIdx = +el.dataset.dragIdx;
                    if (targetIdx !== dragIdx) {
                        dotnetRef.invokeMethodAsync('JsDragEnter', targetIdx);
                        dragIdx = targetIdx;
                        setTimeout(() => { tiles = [...container.querySelectorAll('[data-drag-idx]')]; }, 50);
                    }
                    break;
                }
            }
        }, { passive: false });

        const end = () => {
            clearTimeout(holdTimer);
            if (ghost) { ghost.remove(); ghost = null; }
            tiles.forEach(t => { t.style.opacity = ''; t.style.transform = ''; t.style.boxShadow = ''; });
            if (dragIdx >= 0 && moved) dotnetRef.invokeMethodAsync('JsDragEnd');
            dragIdx = -1; moved = false; held = false;
        };
        container.addEventListener('touchend', end);
        container.addEventListener('touchcancel', end);
    }
};
