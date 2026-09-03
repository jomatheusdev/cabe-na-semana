import type {
    BoardSnapshot,
    KanbanStatus,
    SaveTaskInput
} from "@/features/board/types";

export interface ParsedProblemDetails {
    status: number;
    message: string;
    fieldErrors: Record<string, string[]>;
}

interface ProblemDetailsPayload {
    title?: unknown;
    detail?: unknown;
    errors?: unknown;
}

export class ApiRequestError extends Error {
    readonly status: number;
    readonly fieldErrors: Record<string, string[]>;

    constructor(problem: ParsedProblemDetails) {
        super(problem.message);
        this.name = "ApiRequestError";
        this.status = problem.status;
        this.fieldErrors = problem.fieldErrors;
    }
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null && !Array.isArray(value);
}

function fieldNameFromApi(key: string): string {
    const finalSegment = key.split(".").at(-1) ?? key;
    return finalSegment.length === 0
        ? finalSegment
        : `${finalSegment[0].toLowerCase()}${finalSegment.slice(1)}`;
}

function parseFieldErrors(value: unknown): Record<string, string[]> {
    if (!isRecord(value)) {
        return {};
    }

    return Object.fromEntries(
        Object.entries(value).flatMap(([key, messages]) => {
            if (!Array.isArray(messages)) {
                return [];
            }

            const strings = messages.filter(
                (message): message is string =>
                    typeof message === "string" && message.trim().length > 0
            );

            return strings.length > 0
                ? [[fieldNameFromApi(key), strings] as const]
                : [];
        })
    );
}

export async function readProblemDetails(
    response: Response
): Promise<ParsedProblemDetails> {
    let payload: ProblemDetailsPayload = {};

    try {
        const candidate: unknown = await response.json();
        if (isRecord(candidate)) {
            payload = candidate;
        }
    } catch {
        // A resposta pode vir de um proxy sem um corpo JSON confiável.
    }

    const fieldErrors = parseFieldErrors(payload.errors);
    const firstFieldMessage = Object.values(fieldErrors).flat()[0];
    const detail =
        typeof payload.detail === "string" ? payload.detail.trim() : "";
    const title = typeof payload.title === "string" ? payload.title.trim() : "";
    const message =
        detail ||
        firstFieldMessage ||
        title ||
        `Não foi possível concluir a ação (HTTP ${response.status}).`;

    return {
        status: response.status,
        message,
        fieldErrors
    };
}

function joinUrl(baseUrl: string, path: string): string {
    return baseUrl.length === 0
        ? path
        : `${baseUrl.replace(/\/$/, "")}${path}`;
}

async function request(
    path: string,
    init?: RequestInit,
    baseUrl = ""
): Promise<Response> {
    const response = await fetch(joinUrl(baseUrl, path), {
        ...init,
        headers: {
            accept: "application/json",
            ...(init?.body ? { "content-type": "application/json" } : {}),
            ...init?.headers
        }
    });

    if (!response.ok) {
        throw new ApiRequestError(await readProblemDetails(response));
    }

    return response;
}

export async function getBoard(
    baseUrl = "",
    signal?: AbortSignal
): Promise<BoardSnapshot> {
    const response = await request(
        "/api/board",
        {
            method: "GET",
            cache: "no-store",
            signal
        },
        baseUrl
    );
    return (await response.json()) as BoardSnapshot;
}

export async function createTask(input: SaveTaskInput): Promise<void> {
    await request("/api/tasks", {
        method: "POST",
        body: JSON.stringify(input)
    });
}

export async function updateTask(
    id: string,
    input: SaveTaskInput
): Promise<void> {
    await request(`/api/tasks/${encodeURIComponent(id)}`, {
        method: "PUT",
        body: JSON.stringify(input)
    });
}

export async function moveTask(
    id: string,
    status: KanbanStatus
): Promise<void> {
    await request(`/api/tasks/${encodeURIComponent(id)}/status`, {
        method: "PATCH",
        body: JSON.stringify({ status })
    });
}

export async function deleteTask(id: string): Promise<void> {
    await request(`/api/tasks/${encodeURIComponent(id)}`, {
        method: "DELETE"
    });
}

export async function updateCapacity(
    weeklyCapacityHours: number
): Promise<void> {
    await request("/api/settings/weekly-capacity", {
        method: "PUT",
        body: JSON.stringify({ weeklyCapacityHours })
    });
}
