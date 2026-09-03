"use client";

import { FormEvent, useState } from "react";

import type { CapacitySnapshot } from "./types";

interface CapacityPanelProps {
    capacity: CapacitySnapshot;
    busy?: boolean;
    onSave: (hours: number) => Promise<boolean>;
}

function formatHours(value: number): string {
    return new Intl.NumberFormat("pt-BR", {
        maximumFractionDigits: 2
    }).format(value);
}

export function CapacityPanel({
    capacity,
    busy = false,
    onSave
}: CapacityPanelProps) {
    const [error, setError] = useState("");
    const progressWidth = Math.min(capacity.usagePercentage, 100);

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        const hours = Number(data.get("weeklyCapacityHours"));

        if (!Number.isFinite(hours) || hours < 1 || hours > 80) {
            setError("Informe entre 1 e 80 horas.");
            return;
        }

        setError("");
        await onSave(hours);
    }

    return (
        <section
            className={`capacity-panel${
                capacity.isOverloaded ? " is-overloaded" : ""
            }`}
            aria-labelledby="capacity-title"
        >
            <div className="capacity-heading">
                <div>
                    <span className="section-kicker">Capacidade semanal</span>
                    <h2 id="capacity-title">
                        <strong>{formatHours(capacity.usedHours)} h</strong>
                        <span>de {formatHours(capacity.weeklyHours)} h usadas</span>
                    </h2>
                </div>
                <span className="capacity-percent">
                    {formatHours(capacity.usagePercentage)}%
                </span>
            </div>
            <div
                className="capacity-track"
                role="progressbar"
                aria-label="Capacidade semanal usada"
                aria-valuemin={0}
                aria-valuemax={capacity.weeklyHours}
                aria-valuenow={Math.min(
                    capacity.usedHours,
                    capacity.weeklyHours
                )}
                aria-valuetext={
                    capacity.isOverloaded
                        ? `${formatHours(capacity.usedHours)} de ${formatHours(
                              capacity.weeklyHours
                          )} horas usadas; excesso de ${formatHours(
                              capacity.overbookedHours
                          )} horas`
                        : `${formatHours(capacity.usedHours)} de ${formatHours(
                              capacity.weeklyHours
                          )} horas usadas`
                }
            >
                <span style={{ width: `${progressWidth}%` }} />
            </div>
            <div className="capacity-foot">
                {capacity.isOverloaded ? (
                    <span className="capacity-warning">
                        <span aria-hidden="true">▲</span> Excesso de{" "}
                        {formatHours(capacity.overbookedHours)} h
                    </span>
                ) : (
                    <span>
                        <span aria-hidden="true">○</span>{" "}
                        {formatHours(capacity.availableHours)} h ainda disponíveis
                    </span>
                )}
                <form className="capacity-form" onSubmit={handleSubmit} noValidate>
                    <label htmlFor="weekly-capacity">Ajustar</label>
                    <input
                        id="weekly-capacity"
                        name="weeklyCapacityHours"
                        type="number"
                        min="1"
                        max="80"
                        step="0.5"
                        defaultValue={capacity.weeklyHours}
                        aria-invalid={Boolean(error)}
                        aria-describedby={error ? "capacity-error" : undefined}
                    />
                    <button type="submit" disabled={busy}>
                        {busy ? "…" : "Salvar"}
                    </button>
                    {error && (
                        <span id="capacity-error" className="capacity-error" role="alert">
                            {error}
                        </span>
                    )}
                </form>
            </div>
        </section>
    );
}
