"use client";

import { FormEvent, useId, useRef, useState } from "react";

import { importanceLabels, columnLabels } from "./labels";
import {
    kanbanStatuses,
    type Importance,
    type SaveTaskInput,
    type TaskFormErrors
} from "./types";

interface TaskFormProps {
    defaultDueDate: string;
    initialValue?: SaveTaskInput;
    busy?: boolean;
    submitLabel: string;
    onCancel?: () => void;
    onSubmit: (input: SaveTaskInput) => Promise<boolean | void>;
}

const importanceValues: Importance[] = [
    "low",
    "medium",
    "high",
    "critical"
];

function validate(input: SaveTaskInput): TaskFormErrors {
    const errors: TaskFormErrors = {};

    if (input.title.length === 0) {
        errors.title = ["Informe o título da atividade."];
    } else if (input.title.length > 120) {
        errors.title = ["Use no máximo 120 caracteres."];
    }

    if (!/^\d{4}-\d{2}-\d{2}$/.test(input.dueDate)) {
        errors.dueDate = ["Informe um prazo válido."];
    }

    if (
        !Number.isFinite(input.estimatedHours) ||
        input.estimatedHours < 0.25 ||
        input.estimatedHours > 100
    ) {
        errors.estimatedHours = ["Informe entre 0,25 e 100 horas."];
    }

    return errors;
}

function firstError(errors: TaskFormErrors): keyof TaskFormErrors | undefined {
    return (
        [
            "title",
            "dueDate",
            "importance",
            "estimatedHours",
            "status"
        ] as const
    ).find((field) => Boolean(errors[field]?.length));
}

export function TaskForm({
    defaultDueDate,
    initialValue,
    busy = false,
    submitLabel,
    onCancel,
    onSubmit
}: TaskFormProps) {
    const formRef = useRef<HTMLFormElement>(null);
    const idPrefix = useId();
    const [errors, setErrors] = useState<TaskFormErrors>({});

    const defaults: SaveTaskInput = initialValue ?? {
        title: "",
        dueDate: defaultDueDate,
        importance: "medium",
        estimatedHours: 2,
        status: "planning"
    };

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const form = event.currentTarget;
        const data = new FormData(form);
        const input: SaveTaskInput = {
            title: String(data.get("title") ?? "").trim(),
            dueDate: String(data.get("dueDate") ?? ""),
            importance: String(data.get("importance")) as Importance,
            estimatedHours: Number(data.get("estimatedHours")),
            status: String(data.get("status")) as SaveTaskInput["status"]
        };
        const nextErrors = validate(input);
        setErrors(nextErrors);

        const invalidField = firstError(nextErrors);
        if (invalidField) {
            const field = form.elements.namedItem(invalidField);
            if (field instanceof HTMLElement) {
                field.focus();
            }
            return;
        }

        const succeeded = await onSubmit(input);
        if (succeeded !== false && !initialValue) {
            form.reset();
            setErrors({});
        }
    }

    function errorFor(field: keyof TaskFormErrors): string | undefined {
        return errors[field]?.[0];
    }

    function errorId(field: keyof TaskFormErrors): string {
        return `${idPrefix}-${field}-error`;
    }

    return (
        <form
            ref={formRef}
            className="task-form"
            noValidate
            onSubmit={handleSubmit}
        >
            <div className="field field-wide">
                <label htmlFor={`${idPrefix}-title`}>Atividade</label>
                <input
                    id={`${idPrefix}-title`}
                    name="title"
                    type="text"
                    maxLength={120}
                    defaultValue={defaults.title}
                    placeholder="Ex.: estudar capítulo 4"
                    aria-invalid={Boolean(errorFor("title"))}
                    aria-describedby={
                        errorFor("title") ? errorId("title") : undefined
                    }
                />
                {errorFor("title") && (
                    <span
                        className="field-error"
                        id={errorId("title")}
                        role="alert"
                    >
                        {errorFor("title")}
                    </span>
                )}
            </div>

            <div className="field">
                <label htmlFor={`${idPrefix}-due-date`}>Prazo</label>
                <input
                    id={`${idPrefix}-due-date`}
                    name="dueDate"
                    type="date"
                    defaultValue={defaults.dueDate}
                    aria-invalid={Boolean(errorFor("dueDate"))}
                    aria-describedby={
                        errorFor("dueDate") ? errorId("dueDate") : undefined
                    }
                />
                {errorFor("dueDate") && (
                    <span
                        className="field-error"
                        id={errorId("dueDate")}
                        role="alert"
                    >
                        {errorFor("dueDate")}
                    </span>
                )}
            </div>

            <div className="field">
                <label htmlFor={`${idPrefix}-importance`}>Importância</label>
                <select
                    id={`${idPrefix}-importance`}
                    name="importance"
                    defaultValue={defaults.importance}
                >
                    {importanceValues.map((importance) => (
                        <option key={importance} value={importance}>
                            {importanceLabels[importance]}
                        </option>
                    ))}
                </select>
            </div>

            <div className="field">
                <label htmlFor={`${idPrefix}-estimated-hours`}>
                    Esforço estimado
                </label>
                <div className="input-suffix">
                    <input
                        id={`${idPrefix}-estimated-hours`}
                        name="estimatedHours"
                        type="number"
                        min="0.25"
                        max="100"
                        step="0.25"
                        defaultValue={defaults.estimatedHours}
                        aria-invalid={Boolean(errorFor("estimatedHours"))}
                        aria-describedby={`${idPrefix}-effort-hint${
                            errorFor("estimatedHours")
                                ? ` ${errorId("estimatedHours")}`
                                : ""
                        }`}
                    />
                    <span aria-hidden="true">h</span>
                </div>
                <small id={`${idPrefix}-effort-hint`}>
                    Tempo de foco realista.
                </small>
                {errorFor("estimatedHours") && (
                    <span
                        className="field-error"
                        id={errorId("estimatedHours")}
                        role="alert"
                    >
                        {errorFor("estimatedHours")}
                    </span>
                )}
            </div>

            <div className="field">
                <label htmlFor={`${idPrefix}-status`}>Coluna</label>
                <select
                    id={`${idPrefix}-status`}
                    name="status"
                    defaultValue={defaults.status}
                >
                    {kanbanStatuses.map((status) => (
                        <option key={status} value={status}>
                            {columnLabels[status]}
                        </option>
                    ))}
                </select>
            </div>

            <div className="form-actions field-wide">
                {onCancel && (
                    <button
                        className="secondary-button"
                        type="button"
                        onClick={onCancel}
                    >
                        Cancelar
                    </button>
                )}
                <button
                    className="primary-button"
                    type="submit"
                    disabled={busy}
                >
                    {busy ? "Salvando…" : submitLabel}
                    {!busy && <span aria-hidden="true">→</span>}
                </button>
            </div>
        </form>
    );
}
