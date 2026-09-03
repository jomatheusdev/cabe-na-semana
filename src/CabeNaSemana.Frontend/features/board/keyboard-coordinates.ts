import type { KeyboardCoordinateGetter } from "@dnd-kit/core";

import { kanbanStatuses, type KanbanStatus } from "./types";

const previousKeys = new Set(["ArrowLeft", "ArrowUp"]);
const nextKeys = new Set(["ArrowRight", "ArrowDown"]);

type RectLike = Pick<DOMRect, "left" | "top" | "width">;

function isStatus(value: unknown): value is KanbanStatus {
    return (
        typeof value === "string" &&
        kanbanStatuses.includes(value as KanbanStatus)
    );
}

function fallbackRectFor(status: KanbanStatus): RectLike | undefined {
    const column = document.querySelector<HTMLElement>(
        `[data-kanban-status="${status}"]`
    );

    return column?.getBoundingClientRect();
}

export const kanbanKeyboardCoordinates: KeyboardCoordinateGetter = (
    event,
    { context, currentCoordinates }
) => {
    if (!previousKeys.has(event.code) && !nextKeys.has(event.code)) {
        return undefined;
    }

    event.preventDefault();
    const overStatus: unknown = context.over?.data.current?.status;
    const taskStatus: unknown = context.active?.data.current?.task?.status;
    const currentStatus = isStatus(overStatus)
        ? overStatus
        : isStatus(taskStatus)
          ? taskStatus
          : undefined;

    if (!currentStatus) {
        return currentCoordinates;
    }

    const direction = nextKeys.has(event.code) ? 1 : -1;
    const destinationIndex = kanbanStatuses.indexOf(currentStatus) + direction;
    const destination = kanbanStatuses[destinationIndex];
    if (!destination) {
        return currentCoordinates;
    }

    const destinationRect =
        context.droppableRects.get(`column:${destination}`) ??
        fallbackRectFor(destination);

    if (!destinationRect) {
        return currentCoordinates;
    }

    const draggedWidth = context.collisionRect?.width ?? 0;
    return {
        x:
            destinationRect.left +
            Math.max(0, (destinationRect.width - draggedWidth) / 2),
        y: destinationRect.top
    };
};
