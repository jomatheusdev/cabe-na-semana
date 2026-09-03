import type {
    CapacityFit,
    Importance,
    KanbanStatus,
    PriorityLevel
} from "./types";

export const columnLabels: Record<KanbanStatus, string> = {
    planning: "A planejar",
    thisWeek: "Esta semana",
    inProgress: "Em andamento",
    completed: "Concluído"
};

export const columnHints: Record<KanbanStatus, string> = {
    planning: "Ideias e compromissos ainda sem encaixe.",
    thisWeek: "O que cabe na capacidade desta semana.",
    inProgress: "Poucas coisas, com atenção de verdade.",
    completed: "Entregas que já saíram do caminho."
};

export const importanceLabels: Record<Importance, string> = {
    low: "Baixa",
    medium: "Média",
    high: "Alta",
    critical: "Crítica"
};

export const priorityLabels: Record<PriorityLevel, string> = {
    none: "Sem urgência",
    low: "Baixa",
    medium: "Média",
    high: "Alta",
    urgent: "Urgente"
};

export const capacityLabels: Record<CapacityFit, string> = {
    notScheduled: "Fora do plano semanal",
    fits: "Cabe na semana",
    nearLimit: "Próximo do limite",
    overflow: "Ultrapassa a capacidade",
    completed: "Atividade concluída"
};

export function deadlineLabel(
    dueDate: string,
    daysUntilDue: number,
    completed: boolean
): string {
    const formattedDate = new Intl.DateTimeFormat("pt-BR", {
        day: "2-digit",
        month: "2-digit",
        timeZone: "UTC"
    }).format(new Date(`${dueDate}T00:00:00Z`));

    if (completed) {
        return `Entregue · ${formattedDate}`;
    }

    if (daysUntilDue < 0) {
        const overdueDays = Math.abs(daysUntilDue);
        return `${overdueDays} ${overdueDays === 1 ? "dia" : "dias"} em atraso`;
    }

    if (daysUntilDue === 0) {
        return "Vence hoje";
    }

    if (daysUntilDue === 1) {
        return "Vence amanhã";
    }

    return `${formattedDate} · faltam ${daysUntilDue} dias`;
}
