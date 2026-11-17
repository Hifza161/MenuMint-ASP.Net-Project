(function () {
    'use strict';

    const TYPES = ['image/jpeg', 'image/png', 'image/webp'];

    function init(root) {
        const file = root.querySelector('[data-file]');
        const zone = root.querySelector('[data-dz]');
        const box = root.querySelector('[data-preview]');
        const img = root.querySelector('[data-img]');
        const flag = root.querySelector('[data-removeflag]');
        const maxMb = Number(root.dataset.maxMb) || 2;

        function show(src) {
            img.src = src || '';
            box.style.display = src ? 'block' : 'none';
            zone.style.display = src ? 'none' : 'block';
            root.dispatchEvent(new CustomEvent('uploader:change', { bubbles: true, detail: { src: src || '' } }));
        }

        function handle(f) {
            if (!TYPES.includes(f.type)) { MM.toast('⚠️ Only JPG, PNG or WEBP images.'); file.value = ''; return; }
            if (f.size > maxMb * 1024 * 1024) { MM.toast('⚠️ The image must be ' + maxMb + ' MB or smaller.'); file.value = ''; return; }
            flag.value = 'false';
            const reader = new FileReader();
            reader.onload = e => show(e.target.result);
            reader.readAsDataURL(f);
        }

        file.addEventListener('change', () => { if (file.files[0]) handle(file.files[0]); });

        root.querySelector('[data-remove]').addEventListener('click', () => {
            file.value = '';
            flag.value = 'true';
            show('');
        });

        ['dragenter', 'dragover'].forEach(ev => zone.addEventListener(ev, e => { e.preventDefault(); zone.classList.add('drag'); }));
        ['dragleave', 'drop'].forEach(ev => zone.addEventListener(ev, e => { e.preventDefault(); zone.classList.remove('drag'); }));
        zone.addEventListener('drop', e => {
            if (e.dataTransfer.files.length) { file.files = e.dataTransfer.files; handle(e.dataTransfer.files[0]); }
        });

        show(root.dataset.existing || '');
    }

    document.querySelectorAll('[data-uploader]').forEach(init);
})();