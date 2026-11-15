(function () {
    'use strict';

    /* ---------- Dark mode toggle ---------- */
    const root = document.documentElement;
    const toggle = document.getElementById('themeToggle');

    function syncIcon() {
        if (!toggle) return;
        const dark = root.getAttribute('data-bs-theme') === 'dark';
        toggle.innerHTML = dark ? '<i class="bi bi-sun"></i>' : '<i class="bi bi-moon-stars"></i>';
    }
    syncIcon();

    toggle?.addEventListener('click', () => {
        const next = root.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
        root.setAttribute('data-bs-theme', next);
        try { localStorage.setItem('mm-theme', next); } catch (e) { }
        syncIcon();
    });

    /* ---------- Navbar shadow on scroll ---------- */
    const nav = document.getElementById('mmNav');
    const onScroll = () => nav?.classList.toggle('scrolled', window.scrollY > 8);
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();

    /* ---------- Scroll reveal ---------- */
    const io = new IntersectionObserver((entries) => {
        entries.forEach(e => {
            if (e.isIntersecting) {
                e.target.classList.add('revealed');
                io.unobserve(e.target);
            }
        });
    }, { threshold: 0.12 });
    document.querySelectorAll('[data-reveal]').forEach(el => io.observe(el));

    /* ---------- Toast helper (use anywhere: MM.toast('Saved!')) ---------- */
    window.MM = window.MM || {};
    window.MM.toast = function (msg, ms = 2600) {
        const wrap = document.getElementById('toastWrap');
        if (!wrap) return;
        const t = document.createElement('div');
        t.className = 'mm-toast';
        t.textContent = msg;
        wrap.appendChild(t);
        setTimeout(() => { t.style.opacity = 0; t.style.transition = 'opacity .4s'; }, ms);
        setTimeout(() => t.remove(), ms + 400);
    };
})();