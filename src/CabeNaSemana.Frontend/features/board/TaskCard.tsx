"use client";

import { useDraggable } from "@dnd-kit/core";

import {
    capacityLabels,
    columnLabels,
    deadlineLabel,
    importanceLabels,
    priorityLabels
} from "./labels";
import { kanbanStatuses, type BoardTask, type KanbanStatus } from "./types";

interface TaskCardProps {
    task: BoardTask;
    busy?: boolean;
    overlay?: boolean;
    onDelete?: (task: BoardTask) => void;
    onEdit?: (task: BoardTask) => void;
    onMove?: (
        id: string,
        status: KanbanStatus,
        restoreFocus?: boolean
    ) => Promise<void>;
}

export function TaskCard({
    task,
    busy = false,
    overlay = false,
    onDelete,
    onEdit,
    onMove
}: TaskCardProps) {
    const {
        attributes,
        listeners,
        setActivatorNodeRef,
        setNodeRef,
        transform,
        isDragging
    } = useDraggable({
        id: task.id,
        data: { task },
        disabled: busy || overlay
    });
    const style = transform
        ? {
              transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`
          }
        : undefined;
    const completed = task.status === "completed";

    return (
        <article
            ref={overlay ? undefined : setNodeRef}
            id={overlay ? undefined : `task-card-${task.id}`}
            className={`task-card priority-${task.priorityLevel}${
                isDragging ? " is-dragging" : ""
            }${overlay ? " drag-overlay" : ""}`}
            style={overlay ? undefined : style}
            tabIndex={overlay ? undefined : -1}
            aria-label={overlay ? undefined : `Atividade ${task.title}`}
            aria-hidden={overlay || undefined}
            data-testid="task-card"
        >
            <div className="task-topline">
                <span className="priority-badge">
                    {priorityLabels[task.priorityLevel]} · {task.priorityScore}
                </span>
                <span
                    className={`importance-dot importance-${task.importance}`}
                >
                    {importanceLabels[task.importance]}
                </span>
            </div>
            <div className="task-title-row">
                <h4>{task.title}</h4>
                {!overlay && (
                    <button
                        ref={setActivatorNodeRef}
                        className="drag-handle"
                        type="button"
                        disabled={busy}
                        aria-label={`Arrastar ${task.title}`}
                        {...attributes}
                        {...listeners}
                    >
                        <span aria-hidden="true">⠿</span>
                    </button>
                )}
            </div>
            <p className="priority-explanation">{task.priorityExplanation}</p>
            <dl className="task-meta">
                <div>
                    <dt>Prazo</dt>
                    <dd
                        className={
                            task.daysUntilDue < 0 && !completed ? "is-late" : ""
                        }
                    >
                        {deadlineLabel(task.dueDate, task.daysUntilDue, completed)}
                    </dd>
                </div>
                <div>
                    <dt>Esforço</dt>
                    <dd>{task.estimatedHours.toLocaleString("pt-BR")} h</dd>
                </div>
            </dl>
            <div className={`fit-state fit-${task.capacityFit}`}>
                <span aria-hidden="true" />
                {capacityLabels[task.capacityFit]}
            </div>

            {!overlay && onMove && onEdit && onDelete && (
                <div className="card-actions">
                    <button
                        className="text-action"
                        type="button"
                        onClick={() => onEdit(task)}
                        disabled={busy}
                    >
                        Editar
                    </button>
                    <details className="move-menu">
                        <summary
                            aria-disabled={busy}
                            tabIndex={busy ? -1 : undefined}
                            onClick={
                                busy
                                    ? (event) => event.preventDefault()
                                    : undefined
                            }
                        >
                            Mover
                        </summary>
                        <div className="move-options">
                            {kanbanStatuses
                                .filter((status) => status !== task.status)
                                .map((status) => (
                                    <button
                                        key={status}
                                        type="button"
                                        disabled={busy}
                                        aria-label={`Mover ${task.title} para ${columnLabels[status]}`}
                                        onClick={() =>
                                            void onMove(task.id, status, true)
                                        }
                                    >
                                        {columnLabels[status]}
                                    </button>
                                ))}
                        </div>
                    </details>
                    <button
                        className="delete-button"
                        type="button"
                        onClick={() => onDelete(task)}
                        disabled={busy}
                    >
                        Excluir
                    </button>
                </div>
            )}
        </article>
    );
}
