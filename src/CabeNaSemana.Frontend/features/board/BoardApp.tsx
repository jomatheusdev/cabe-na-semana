"use client";

import { useCallback, useRef, useState } from "react";

import {
    createTask,
    deleteTask,
    getBoard,
    moveTask,
    updateCapacity,
    updateTask
} from "@/lib/api-client";

import { moveTaskLocally } from "./board-state";
import { CapacityPanel } from "./CapacityPanel";
import { columnLabels } from "./labels";
import { EditTaskDialog } from "./EditTaskDialog";
import { KanbanBoard } from "./KanbanBoard";
import { TaskForm } from "./TaskForm";
import type {
    BoardSnapshot,
    BoardTask,
    KanbanStatus,
    SaveTaskInput
} from "./types";

interface BoardAppProps {
    initialBoard: BoardSnapshot;
}

interface Feedback {
    kind: "success" | "error";
    message: string;
}

type MutationKind = "create" | "move" | "update" | "delete" | "capacity";

interface ActiveMutation {
    kind: MutationKind;
    taskId?: string;
}

function errorMessage(error: unknown): string {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : "Não foi possível concluir a ação.";
}

function addDays(date: string, days: number): string {
    const value = new Date(`${date}T00:00:00Z`);
    value.setUTCDate(value.getUTCDate() + days);
    return value.toISOString().slice(0, 10);
}

function focusTaskCard(taskId: string): void {
    window.setTimeout(() => {
        document.getElementById(`task-card-${taskId}`)?.focus();
    }, 0);
}

export function BoardApp({ initialBoard }: BoardAppProps) {
    const [board, setBoard] = useState(initialBoard);
    const [feedback, setFeedback] = useState<Feedback>();
    const [activeMutation, setActiveMutation] = useState<ActiveMutation>();
    const [editingTask, setEditingTask] = useState<BoardTask>();
    const mutationLockRef = useRef(false);

    const refreshBoard = useCallback(async () => {
        const refreshed = await getBoard();
        setBoard(refreshed);
    }, []);

    function beginMutation(kind: MutationKind, taskId?: string): boolean {
        if (mutationLockRef.current) {
            setFeedback({
                kind: "error",
                message: "Aguarde a alteração em andamento antes de iniciar outra."
            });
            return false;
        }

        mutationLockRef.current = true;
        setActiveMutation({ kind, taskId });
        setFeedback(undefined);
        return true;
    }

    function finishMutation(): void {
        mutationLockRef.current = false;
        setActiveMutation(undefined);
    }

    async function refreshAfterCommittedMutation(
        successMessage: string
    ): Promise<void> {
        try {
            await refreshBoard();
            setFeedback({ kind: "success", message: successMessage });
        } catch {
            setFeedback({
                kind: "error",
                message: `${successMessage} A alteração foi salva, mas não foi possível atualizar o quadro. Recarregue a página para sincronizar os dados.`
            });
        }
    }

    async function handleCreate(input: SaveTaskInput): Promise<boolean> {
        if (!beginMutation("create")) {
            return false;
        }

        try {
            await createTask(input);
            await refreshAfterCommittedMutation("Atividade adicionada ao plano.");
            return true;
        } catch (error) {
            setFeedback({ kind: "error", message: errorMessage(error) });
            return false;
        } finally {
            finishMutation();
        }
    }

    async function handleMove(
        taskId: string,
        destination: KanbanStatus,
        restoreFocus = false
    ): Promise<void> {
        const previous = board;
        const movingTask = previous.columns
            .flatMap((column) => column.tasks)
            .find((task) => task.id === taskId);
        if (!movingTask || movingTask.status === destination) {
            return;
        }

        if (!beginMutation("move", taskId)) {
            return;
        }

        setBoard(moveTaskLocally(previous, taskId, destination));
        if (restoreFocus) {
            focusTaskCard(taskId);
        }

        try {
            await moveTask(taskId, destination);
            await refreshAfterCommittedMutation(
                `${movingTask.title} foi movida para ${columnLabels[destination]}.`
            );
        } catch (error) {
            setBoard(previous);
            if (restoreFocus) {
                focusTaskCard(taskId);
            }
            setFeedback({ kind: "error", message: errorMessage(error) });
        } finally {
            finishMutation();
        }
    }

    async function handleUpdate(input: SaveTaskInput): Promise<boolean> {
        if (!editingTask) {
            return false;
        }

        const taskId = editingTask.id;
        if (!beginMutation("update", taskId)) {
            return false;
        }

        try {
            await updateTask(taskId, input);
            await refreshAfterCommittedMutation("Atividade atualizada.");
            return true;
        } catch (error) {
            setFeedback({ kind: "error", message: errorMessage(error) });
            return false;
        } finally {
            finishMutation();
        }
    }

    async function handleDelete(task: BoardTask): Promise<void> {
        const confirmed = window.confirm(
            `Excluir “${task.title}”? Esta ação não pode ser desfeita.`
        );
        if (!confirmed) {
            return;
        }

        if (!beginMutation("delete", task.id)) {
            return;
        }

        try {
            await deleteTask(task.id);
            await refreshAfterCommittedMutation("Atividade excluída.");
        } catch (error) {
            setFeedback({ kind: "error", message: errorMessage(error) });
        } finally {
            finishMutation();
        }
    }

    async function handleCapacity(hours: number): Promise<boolean> {
        if (!beginMutation("capacity")) {
            return false;
        }

        try {
            await updateCapacity(hours);
            await refreshAfterCommittedMutation(
                "Capacidade semanal atualizada."
            );
            return true;
        } catch (error) {
            setFeedback({ kind: "error", message: errorMessage(error) });
            return false;
        } finally {
            finishMutation();
        }
    }

    const isMutating = activeMutation !== undefined;
    const busyTaskId = activeMutation?.taskId;
    const creating = activeMutation?.kind === "create";
    const savingCapacity = activeMutation?.kind === "capacity";

    const completionRate =
        board.totalTasks === 0
            ? 0
            : Math.round((board.completedTasks * 100) / board.totalTasks);

    return (
        <main id="main-content" className="page-shell" tabIndex={-1}>
            {feedback && (
                <div
                    className={`flash flash-${feedback.kind}`}
                    role={feedback.kind === "error" ? "alert" : "status"}
                    aria-live={feedback.kind === "error" ? "assertive" : "polite"}
                >
                    <span aria-hidden="true">
                        {feedback.kind === "error" ? "!" : "✓"}
                    </span>
                    {feedback.message}
                </div>
            )}

            <section className="dashboard-head" aria-labelledby="page-title">
                <div className="title-block">
                    <p className="eyebrow">Semana em perspectiva</p>
                    <h1 id="page-title">O que realmente cabe?</h1>
                    <p>
                        Priorize pelo que importa, reconheça os limites e ajuste
                        o plano antes que a semana ajuste você.
                    </p>
                </div>

                <CapacityPanel
                    capacity={board.capacity}
                    busy={savingCapacity}
                    onSave={handleCapacity}
                />
            </section>

            {board.capacity.isOverloaded && (
                <section
                    className="overload-alert"
                    role="alert"
                    aria-labelledby="overload-title"
                >
                    <span className="alert-icon" aria-hidden="true">
                        ↯
                    </span>
                    <div>
                        <h2 id="overload-title">A conta não fecha ainda</h2>
                        <p>
                            As atividades desta semana somam{" "}
                            <strong>{board.capacity.usedHours} h</strong>. Mova,
                            reduza ou conclua{" "}
                            <strong>{board.capacity.overbookedHours} h</strong>{" "}
                            para voltar ao limite.
                        </p>
                    </div>
                </section>
            )}

            <section className="workbench" aria-label="Planejamento da semana">
                <aside className="quick-add" aria-labelledby="quick-add-title">
                    <div className="quick-add-head">
                        <span className="step-number" aria-hidden="true">
                            +
                        </span>
                        <div>
                            <p className="eyebrow">Captura rápida</p>
                            <h2 id="quick-add-title">Nova atividade</h2>
                        </div>
                    </div>
                    <TaskForm
                        defaultDueDate={addDays(board.today, 7)}
                        busy={creating}
                        submitLabel="Adicionar ao quadro"
                        onSubmit={handleCreate}
                    />
                </aside>

                <section className="week-glance" aria-labelledby="glance-title">
                    <div>
                        <p className="eyebrow">Resumo visual</p>
                        <h2 id="glance-title">Ritmo da semana</h2>
                    </div>
                    <div className="glance-grid">
                        <div>
                            <strong>{board.totalTasks}</strong>
                            <span>atividades</span>
                        </div>
                        <div>
                            <strong>{board.completedTasks}</strong>
                            <span>concluídas</span>
                        </div>
                        <div>
                            <strong>{completionRate}%</strong>
                            <span>do quadro</span>
                        </div>
                    </div>
                    <p className="glance-note">
                        A prioridade combina importância, prazo e esforço. Ela
                        orienta; você continua no comando.
                    </p>
                </section>
            </section>

            <section className="board-section" aria-labelledby="board-title">
                <div className="board-heading">
                    <div>
                        <p className="eyebrow">Plano revisável</p>
                        <h2 id="board-title">Quadro da semana</h2>
                    </div>
                    <p>
                        Arraste pela alça ou use “Mover”. As duas opções funcionam
                        com mouse, toque e teclado.
                    </p>
                </div>

                <KanbanBoard
                    board={board}
                    busy={isMutating}
                    busyTaskId={busyTaskId}
                    onDelete={(task) => void handleDelete(task)}
                    onEdit={setEditingTask}
                    onMove={handleMove}
                />
            </section>

            {editingTask && (
                <EditTaskDialog
                    task={editingTask}
                    busy={isMutating}
                    onClose={() => setEditingTask(undefined)}
                    onSave={handleUpdate}
                />
            )}
        </main>
    );
}
