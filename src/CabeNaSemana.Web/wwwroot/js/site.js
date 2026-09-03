(() => {
    const firstInvalidField = document.querySelector('[aria-invalid="true"]');
    if (firstInvalidField instanceof HTMLElement) {
        firstInvalidField.focus();
    }

    for (const form of document.querySelectorAll('form[data-confirm]')) {
        form.addEventListener('submit', event => {
            const message = form.getAttribute('data-confirm') ?? 'Confirmar esta ação?';
            if (!window.confirm(message)) {
                event.preventDefault();
            }
        });
    }

    document.addEventListener('click', event => {
        const target = event.target;
        if (!(target instanceof Node)) {
            return;
        }

        for (const details of document.querySelectorAll('.move-menu[open]')) {
            if (!details.contains(target)) {
                details.removeAttribute('open');
            }
        }
    });
})();
