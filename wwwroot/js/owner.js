(function () {
    'use strict';

    /* ---------- Mobile sidebar ---------- */
    const body = document.body;
    const toggle = document.getElementById('sideToggle');
    const overlay = document.getElementById('sideOverlay');
    const close = () => body.classList.remove('side-open');

    toggle?.addEventListener('click', () => body.classList.toggle('side-open'));
    overlay?.addEventListener('click', close);
    document.addEventListener('keydown', e => { if (e.key === 'Escape') close(); });

    /* ---------- Count-up numbers ---------- */
    document.querySelectorAll('[data-count]').forEach(el => {
        const target = +el.dataset.count || 0;
        const duration = 900;
        const start = performance.now();
        const tick = now => {
            const t = Math.min((now - start) / duration, 1);
            const eased = 1 - Math.pow(1 - t, 3);
            el.textContent = Math.round(target * eased);
            if (t < 1) requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
    });

    /* ---------- Progress ring ---------- */
    document.querySelectorAll('.ring[data-percent]').forEach(ring => {
        const pct = +ring.dataset.percent || 0;
        setTimeout(() => ring.style.setProperty('--p', pct), 250);
    });

    /* ---------- Copy live link ---------- */
    const copyBtn = document.getElementById('copyLive');
    const liveUrl = document.getElementById('liveUrl');
    copyBtn?.addEventListener('click', async () => {
        try {
            await navigator.clipboard.writeText(liveUrl.value);
        } catch {
            liveUrl.select();
            document.execCommand('copy');
        }
        copyBtn.innerHTML = '<i class="bi bi-check2"></i> Copied';
        MM.toast('Menu link copied 🔗');
        setTimeout(() => copyBtn.innerHTML = '<i class="bi bi-clipboard"></i> Copy', 1800);
    });
})();