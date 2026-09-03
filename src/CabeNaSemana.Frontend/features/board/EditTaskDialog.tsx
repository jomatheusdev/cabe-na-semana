"use client";

import { useEffect, useRef } from "react";

import type { BoardTask, SaveTaskInput } from "./types";
import { TaskForm } from "./TaskForm";

interface EditTaskDialogProps {
    task: BoardTask;
    busy?: boolean;
    onClose: () => void;
    onSave: (input: SaveTaskInput) => Promise<boolean>;
}

export function EditTaskDialog({
    task,
    busy = false,
    onClose,
    onSave
}: EditTaskDialogProps) {
    const dialogRef = useRef<HTMLDialogElement>(null);

    useEffect(() => {
        const dialog = dialogRef.current;
        if (dialog && !dialog.open) {
            dialog.showModal();
        }
    }, []);

    async function handleSave(input: SaveTaskInput): Promise<boolean> {
        const succeeded = await onSave(input);
        if (succeeded) {
            dialogRef.current?.close();
        }
        return succeeded;
    }

    return (
        <dialog
            ref={dialogRef}
            className="edit-dialog"
            aria-labelledby="edit-dialog-title"
            onClose={onClose}
        >
            <div className="edit-dialog-head">
                <div>
                    <p className="eyebrow">Revisar o plano</p>
                    <h2 id="edit-dialog-title">Editar atividade</h2>
                    <p>
                        Ao salvar, prioridade e capacidade são recalculadas pelo
                        domínio.
                    </p>
                </div>
                <button
                    className="dialog-close"
                    type="button"
                    aria-label="Fechar edição"
                    onClick={() => dialogRef.current?.close()}
                >
                    ×
                </button>
            </div>
            <TaskForm
                key={task.id}
                defaultDueDate={task.dueDate}
                initialValue={{
                    title: task.title,
                    dueDate: task.dueDate,
                    importance: task.importance,
                    estimatedHours: task.estimatedHours,
                    status: task.status
                }}
                busy={busy}
                submitLabel="Salvar alterações"
                onCancel={() => dialogRef.current?.close()}
                onSubmit={handleSave}
            />
        </dialog>
    );
}
