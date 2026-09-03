import { afterEach, describe, expect, it, vi } from "vitest";

import { boardFixture } from "@/tests/fixtures/board";

import {
    ApiRequestError,
    createTask,
    deleteTask,
    getBoard,
    moveTask,
    readProblemDetails,
    updateCapacity,
    updateTask
} from "../api-client";

afterEach(() => {
    vi.unstubAllGlobals();
});

describe("readProblemDetails", () => {
    it("extrai mensagem e erros por campo do ValidationProblemDetails", async () => {
        const response = new Response(
            JSON.stringify({
                title: "A validação falhou.",
                status: 400,
                errors: {
                    Title: ["Informe o título da atividade."],
                    EstimatedHours: ["Informe entre 0,25 e 100 horas."]
                }
            }),
            {
                status: 400,
                headers: { "content-type": "application/problem+json" }
            }
        );

        const problem = await readProblemDetails(response);

        expect(problem.message).toBe("Informe o título da atividade.");
        expect(problem.fieldErrors).toEqual({
            title: ["Informe o título da atividade."],
            estimatedHours: ["Informe entre 0,25 e 100 horas."]
        });
    });

    it("prioriza detail e cria uma mensagem segura quando o corpo não é JSON", async () => {
        const detailed = new Response(
            JSON.stringify({
                title: "Regra de negócio inválida.",
                detail: "A tarefa não pode ser concluída neste estado.",
                status: 422
            }),
            { status: 422 }
        );
        const invalid = new Response("gateway indisponível", { status: 503 });

        await expect(readProblemDetails(detailed)).resolves.toMatchObject({
            message: "A tarefa não pode ser concluída neste estado.",
            status: 422
        });
        await expect(readProblemDetails(invalid)).resolves.toMatchObject({
            message: "Não foi possível concluir a ação (HTTP 503).",
            status: 503,
            fieldErrors: {}
        });
    });

    it("usa o título quando não há detalhe nem erro de campo", async () => {
        const response = new Response(
            JSON.stringify({ title: "Recurso não encontrado.", status: 404 }),
            { status: 404 }
        );

        await expect(readProblemDetails(response)).resolves.toMatchObject({
            message: "Recurso não encontrado.",
            status: 404
        });
    });

    it("busca o quadro pela URL interna sem cache", async () => {
        const fetchMock = vi
            .fn()
            .mockResolvedValue(
                new Response(JSON.stringify(boardFixture), { status: 200 })
            );
        vi.stubGlobal("fetch", fetchMock);

        await expect(getBoard("http://api:8080/")).resolves.toEqual(boardFixture);
        expect(fetchMock).toHaveBeenCalledWith(
            "http://api:8080/api/board",
            expect.objectContaining({ method: "GET", cache: "no-store" })
        );
    });

    it("envia os cinco comandos de escrita para as rotas sem versão", async () => {
        const fetchMock = vi
            .fn()
            .mockResolvedValue(new Response(null, { status: 204 }));
        vi.stubGlobal("fetch", fetchMock);
        const input = {
            title: "Preparar demo",
            dueDate: "2026-09-12",
            importance: "critical" as const,
            estimatedHours: 4.5,
            status: "thisWeek" as const
        };

        await createTask(input);
        await updateTask("id com espaço", input);
        await moveTask("task-id", "inProgress");
        await deleteTask("task-id");
        await updateCapacity(20);

        expect(fetchMock).toHaveBeenNthCalledWith(
            1,
            "/api/tasks",
            expect.objectContaining({
                method: "POST",
                body: JSON.stringify(input)
            })
        );
        expect(fetchMock).toHaveBeenNthCalledWith(
            2,
            "/api/tasks/id%20com%20espa%C3%A7o",
            expect.objectContaining({ method: "PUT" })
        );
        expect(fetchMock).toHaveBeenNthCalledWith(
            3,
            "/api/tasks/task-id/status",
            expect.objectContaining({
                method: "PATCH",
                body: JSON.stringify({ status: "inProgress" })
            })
        );
        expect(fetchMock).toHaveBeenNthCalledWith(
            4,
            "/api/tasks/task-id",
            expect.objectContaining({ method: "DELETE" })
        );
        expect(fetchMock).toHaveBeenNthCalledWith(
            5,
            "/api/settings/weekly-capacity",
            expect.objectContaining({
                method: "PUT",
                body: JSON.stringify({ weeklyCapacityHours: 20 })
            })
        );
    });

    it("transforma uma resposta HTTP inválida em ApiRequestError", async () => {
        vi.stubGlobal(
            "fetch",
            vi.fn().mockResolvedValue(
                new Response(
                    JSON.stringify({
                        detail: "A regra do domínio recusou a alteração.",
                        status: 422
                    }),
                    { status: 422 }
                )
            )
        );

        const result = createTask({
            title: "Inválida",
            dueDate: "2026-09-12",
            importance: "medium",
            estimatedHours: 2,
            status: "planning"
        });

        await expect(result).rejects.toEqual(
            expect.objectContaining({
                name: "ApiRequestError",
                status: 422,
                message: "A regra do domínio recusou a alteração."
            })
        );
        await expect(result).rejects.toBeInstanceOf(ApiRequestError);
    });
});
