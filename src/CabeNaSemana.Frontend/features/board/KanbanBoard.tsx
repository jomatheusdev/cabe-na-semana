"use client";

import {
    closestCenter,
    DndContext,
    DragEndEvent,
    DragOverlay,
    DragStartEvent,
    KeyboardSensor,
    PointerSensor,
    TouchSensor,
    useSensor,
    useSensors
} from "@dnd-kit/core";
import { useMemo, useState } from "react";

import { findTask } from "./board-state";
import { kanbanKeyboardCoordinates } from "./keyboard-coordinates";
import { columnLabels } from "./labels";
import type {
    BoardSnapshot,
    BoardTask,
    KanbanStatus
} from "./types";
import { KanbanColumnView } from "./KanbanColumn";
import { TaskCard } from "./TaskCard";

interface KanbanBoardProps {
    board: BoardSnapshot;
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

function statusFromEvent(event: DragEndEvent): KanbanStatus | undefined {
    const status: unknown = event.over?.data.current?.status;
    return typeof status === "string" && status in columnLabels
        ? (status as KanbanStatus)
        : undefined;
}

export function KanbanBoard({
    board,
    busy = false,
    busyTaskId,
    onDelete,
    onEdit,
    onMove
}: KanbanBoardProps) {
    const [activeTaskId, setActiveTaskId] = useState<string>();
    const sensors = useSensors(
        useSensor(PointerSensor, {
            activationConstraint: { distance: 6 }
        }),
        useSensor(TouchSensor, {
            activationConstraint: { delay: 180, tolerance: 6 }
        }),
        useSensor(KeyboardSensor, {
            coordinateGetter: kanbanKeyboardCoordinates
        })
    );
    const activeTask = activeTaskId
        ? findTask(board, activeTaskId)
        : undefined;
    const accessibility = useMemo(
        () => ({
            screenReaderInstructions: {
                draggable:
                    "Para mover o cartão, pressione Espaço. Use as setas para escolher uma coluna, Espaço para soltar ou Escape para cancelar. Os botões Mover também realizam a mesma ação."
            },
            announcements: {
                onDragStart({ active }: { active: { id: string | number } }) {
                    const task = findTask(board, String(active.id));
                    return task
                        ? `${task.title} selecionada. Escolha a coluna de destino.`
                        : "Cartão selecionado.";
                },
                onDragOver({
                    active,
                    over
                }: {
                    active: { id: string | number };
                    over: { data: { current?: { status?: KanbanStatus } } } | null;
                }) {
                    const task = findTask(board, String(active.id));
                    const status = over?.data.current?.status;
                    return task && status
                        ? `${task.title} sobre ${columnLabels[status]}.`
                        : undefined;
                },
                onDragEnd({
                    active,
                    over
                }: {
                    active: { id: string | number };
                    over: { data: { current?: { status?: KanbanStatus } } } | null;
                }) {
                    const task = findTask(board, String(active.id));
                    const status = over?.data.current?.status;
                    return task && status
                        ? `${task.title} solta em ${columnLabels[status]}.`
                        : "Movimento cancelado.";
                },
                onDragCancel() {
                    return "Movimento cancelado.";
                },
                onDragMove() {
                    return undefined;
                }
            }
        }),
        [board]
    );

    function handleDragStart(event: DragStartEvent) {
        setActiveTaskId(String(event.active.id));
    }

    function handleDragEnd(event: DragEndEvent) {
        setActiveTaskId(undefined);
        const destination = statusFromEvent(event);
        const taskId = String(event.active.id);
        const task = findTask(board, taskId);

        if (destination && task && destination !== task.status) {
            void onMove(taskId, destination);
        }
    }

    return (
        <DndContext
            id="weekly-board-dnd"
            sensors={sensors}
            collisionDetection={closestCenter}
            accessibility={accessibility}
            onDragStart={handleDragStart}
            onDragCancel={() => setActiveTaskId(undefined)}
            onDragEnd={handleDragEnd}
        >
            <p id="drag-instructions" className="visually-hidden">
                Arraste pelo botão de alça. Com teclado, pressione Espaço, mova
                com as setas e pressione Espaço novamente. Use os botões Mover
                se preferir.
            </p>
            <div className="kanban-board">
                {board.columns.map((column) => (
                    <KanbanColumnView
                        key={column.status}
                        column={column}
                        busy={busy}
                        busyTaskId={busyTaskId}
                        onDelete={onDelete}
                        onEdit={onEdit}
                        onMove={onMove}
                    />
                ))}
            </div>
            <DragOverlay dropAnimation={null}>
                {activeTask ? <TaskCard task={activeTask} overlay /> : null}
            </DragOverlay>
        </DndContext>
    );
}
