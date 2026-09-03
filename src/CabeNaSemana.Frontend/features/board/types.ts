export const kanbanStatuses = [
    "planning",
    "thisWeek",
    "inProgress",
    "completed"
] as const;

export type KanbanStatus = (typeof kanbanStatuses)[number];
export type Importance = "low" | "medium" | "high" | "critical";
export type PriorityLevel = "none" | "low" | "medium" | "high" | "urgent";
export type CapacityFit =
    | "notScheduled"
    | "fits"
    | "nearLimit"
    | "overflow"
    | "completed";

export interface BoardTask {
    id: string;
    title: string;
    dueDate: string;
    importance: Importance;
    estimatedHours: number;
    status: KanbanStatus;
    priorityScore: number;
    priorityLevel: PriorityLevel;
    priorityExplanation: string;
    daysUntilDue: number;
    capacityFit: CapacityFit;
}

export interface BoardColumn {
    status: KanbanStatus;
    tasks: BoardTask[];
}

export interface CapacitySnapshot {
    weeklyHours: number;
    usedHours: number;
    availableHours: number;
    overbookedHours: number;
    usagePercentage: number;
    isOverloaded: boolean;
}

export interface BoardSnapshot {
    columns: BoardColumn[];
    capacity: CapacitySnapshot;
    today: string;
    totalTasks: number;
    completedTasks: number;
}

export interface SaveTaskInput {
    title: string;
    dueDate: string;
    importance: Importance;
    estimatedHours: number;
    status: KanbanStatus;
}

export interface TaskFormErrors {
    title?: string[];
    dueDate?: string[];
    importance?: string[];
    estimatedHours?: string[];
    status?: string[];
}
