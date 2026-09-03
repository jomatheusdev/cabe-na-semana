import type { Metadata } from "next";
import Link from "next/link";
import type { ReactNode } from "react";

import "./globals.css";

export const metadata: Metadata = {
    title: {
        default: "Cabe na Semana",
        template: "%s · Cabe na Semana"
    },
    description:
        "Planejador semanal que combina prioridade, esforço e capacidade real."
};

export default function RootLayout({ children }: Readonly<{ children: ReactNode }>) {
    return (
        <html lang="pt-BR">
            <body>
                <a className="skip-link" href="#main-content">
                    Pular para o conteúdo
                </a>
                <header className="site-header">
                    <Link className="brand" href="/" aria-label="Cabe na Semana, início">
                        <span className="brand-mark" aria-hidden="true">
                            C
                        </span>
                        <span>
                            <strong>Cabe na Semana</strong>
                            <small>Planejamento honesto</small>
                        </span>
                    </Link>
                    <span className="local-note">MVP · uma semana de cada vez</span>
                </header>
                {children}
                <footer className="site-footer">
                    <p>
                        Planeje com limites reais. A prioridade orienta; você decide.
                    </p>
                </footer>
            </body>
        </html>
    );
}
