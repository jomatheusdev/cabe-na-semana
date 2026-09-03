import type {
    BoardSnapshot,
    BoardTask,
    KanbanStatus
} from "./types";

export function findTask(
    board: BoardSnapshot,
    taskId: string
): BoardTask | undefined {
    return board.columns
        .flatMap((column) => column.tasks)
        .find((task) => task.id === taskId);
}

export function moveTaskLocally(
    board: BoardSnapshot,
    taskId: string,
    destination: KanbanStatus
): BoardSnapshot {
    const task = findTask(board, taskId);

    if (!task || task.status === destination) {
        return board;
    }

    return {
        ...board,
        completedTasks:
            board.completedTasks +
            (destination === "completed" ? 1 : 0) -
            (task.status === "completed" ? 1 : 0),
        columns: board.columns.map((column) => {
            const tasksWithoutMoved = column.tasks.filter(
                (candidate) => candidate.id !== taskId
            );

            if (column.status !== destination) {
                return {
                    ...column,
                    tasks: tasksWithoutMoved
                };
            }

            return {
                ...column,
                tasks: [
                    ...tasksWithoutMoved,
                    {
                        ...task,
                        status: destination,
                        capacityFit:
                            destination === "completed"
                                ? "completed"
                                : task.capacityFit
                    }
                ]
            };
        })
    };
}
