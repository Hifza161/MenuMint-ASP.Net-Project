(function () {
    'use strict';

    /* ---------- Show / hide password ---------- */
    document.querySelectorAll('[data-toggle-password]').forEach(btn => {
        btn.addEventListener('click', () => {
            const input = btn.parentElement.querySelector('input');
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            btn.innerHTML = show ? '<i class="bi bi-eye-slash"></i>' : '<i class="bi bi-eye"></i>';
            btn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
        });
    });

    /* ---------- Password strength meter ---------- */
    const pw = document.getElementById('pw');
    const meter = document.getElementById('strength');
    const label = document.getElementById('strengthLabel');
    if (pw && meter) {
        const names = ['Enter a password', 'Weak', 'Okay', 'Good', 'Strong'];
        const score = v => {
            if (!v) return 0;
            let s = 0;
            if (v.length >= 8) s++;
            if (/[a-z]/.test(v) && /[A-Z]/.test(v)) s++;
            if (/\d/.test(v)) s++;
            if (/[^A-Za-z0-9]/.test(v) || v.length >= 12) s++;
            return Math.max(s, 1);
        };
        pw.addEventListener('input', () => {
            const s = score(pw.value);
            meter.dataset.level = s;
            label.textContent = names[s];
        });
    }

    /* ---------- Live slug preview (same rules as SlugService on the server) ---------- */
    const rn = document.getElementById('restaurantName');
    const slugText = document.getElementById('slugText');
    if (rn && slugText) {
        const slugify = s => s.normalize('NFD').replace(/[\u0300-\u036f]/g, '')   // Café -> Cafe
            .toLowerCase().trim()
            .replace(/&/g, ' and ')
            .replace(/[^a-z0-9\s-]/g, '')
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-')
            .replace(/^-|-$/g, '');
        const update = () => {
            slugText.textContent = location.host + '/m/' + (slugify(rn.value) || 'your-restaurant');
        };
        rn.addEventListener('input', update);
        update();
    }
})();