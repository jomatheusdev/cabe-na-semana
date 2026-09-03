"use client";

import { useDroppable } from "@dnd-kit/core";

import { columnHints, columnLabels } from "./labels";
import type { BoardColumn, BoardTask, KanbanStatus } from "./types";
import { TaskCard } from "./TaskCard";

interface KanbanColumnViewProps {
    column: BoardColumn;
    busy?: boolean;
    busyTaskId?: string;
    onDelete: (task: BoardTask) => void;
    onEdit: (task: BoardTask) => void;
    onMove: (
        id: string,
        status: KanbanStatus,
        restoreFocus?: boolean
    ) => Promise<void>;
}

export function KanbanColumnView({
    column,
    busy = false,
    busyTaskId,
    onDelete,
    onEdit,
    onMove
}: KanbanColumnViewProps) {
    const { isOver, setNodeRef } = useDroppable({
        id: `column:${column.status}`,
        data: { status: column.status }
    });
    const titleId = `column-${column.status}-title`;

    return (
        <section
            ref={setNodeRef}
            className={`kanban-column column-${column.status}${
                isOver ? " is-drop-target" : ""
            }`}
            data-kanban-status={column.status}
            aria-labelledby={titleId}
            aria-label={columnLabels[column.status]}
        >
            <header className="column-head">
                <div>
                    <h3 id={titleId}>{columnLabels[column.status]}</h3>
                    <p>{columnHints[column.status]}</p>
                </div>
                <span
                    className="column-count"
                    aria-label={`Total nesta coluna: ${column.tasks.length}`}
                >
                    {column.tasks.length}
                </span>
            </header>

            <div className="task-list">
                {column.tasks.length === 0 && (
                    <div className="empty-state">
                        <span aria-hidden="true">✦</span>
                        <p>Nada por aqui.</p>
                        <small>
                            Arraste ou mova uma atividade para esta etapa.
                        </small>
                    </div>
                )}
                {column.tasks.map((task) => (
                    <TaskCard
                        key={task.id}
                        task={task}
                        busy={busy || busyTaskId === task.id}
                        onDelete={onDelete}
                        onEdit={onEdit}
                        onMove={onMove}
                    />
                ))}
            </div>
        </section>
    );
}
