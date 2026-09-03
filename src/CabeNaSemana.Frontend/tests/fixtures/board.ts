import type { BoardSnapshot } from "@/features/board/types";

export const boardFixture: BoardSnapshot = {
    today: "2026-09-03",
    totalTasks: 1,
    completedTasks: 0,
    capacity: {
        weeklyHours: 15,
        usedHours: 3,
        availableHours: 12,
        overbookedHours: 0,
        usagePercentage: 20,
        isOverloaded: false
    },
    columns: [
        {
            status: "planning",
            tasks: [
                {
                    id: "cd53b05d-dad0-4436-bf61-7a926310540c",
                    title: "Revisar API",
                    dueDate: "2026-09-08",
                    importance: "high",
                    estimatedHours: 3,
                    status: "planning",
                    priorityScore: 74,
                    priorityLevel: "high",
                    priorityExplanation: "Alta importância e prazo próximo.",
                    daysUntilDue: 5,
                    capacityFit: "notScheduled"
                }
            ]
        },
        { status: "thisWeek", tasks: [] },
        { status: "inProgress", tasks: [] },
        { status: "completed", tasks: [] }
    ]
};
