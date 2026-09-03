export default function Loading() {
    return (
        <main id="main-content" className="page-shell" tabIndex={-1}>
            <section className="loading-panel" role="status" aria-live="polite">
                <span className="loading-mark" aria-hidden="true" />
                <div>
                    <p className="eyebrow">Organizando prioridades</p>
                    <h1>Montando seu quadro…</h1>
                </div>
            </section>
        </main>
    );
}
