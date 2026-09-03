"use client";

export default function ErrorPage({ reset }: { reset: () => void }) {
    return (
        <main id="main-content" className="page-shell error-shell" tabIndex={-1}>
            <section className="error-panel" role="alert">
                <p className="eyebrow">Algo saiu do plano</p>
                <h1>Não foi possível carregar o quadro.</h1>
                <p>
                    Verifique se a API está disponível e tente novamente. Seus
                    dados continuam salvos no banco.
                </p>
                <button className="primary-button" type="button" onClick={reset}>
                    Tentar novamente
                </button>
            </section>
        </main>
    );
}
