import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeAll, beforeEach, describe, expect, it, vi } from "vitest";

import { boardFixture } from "@/tests/fixtures/board";

import { BoardApp } from "../BoardApp";
import { moveTaskLocally } from "../board-state";

const apiMocks = vi.hoisted(() => ({
    getBoard: vi.fn(),
    moveTask: vi.fn(),
    createTask: vi.fn(),
    updateTask: vi.fn(),
    deleteTask: vi.fn(),
    updateCapacity: vi.fn()
}));

vi.mock("@/lib/api-client", () => apiMocks);

describe("BoardApp", () => {
    beforeAll(() => {
        Object.defineProperty(HTMLDialogElement.prototype, "showModal", {
            configurable: true,
            value(this: HTMLDialogElement) {
                this.setAttribute("open", "");
            }
        });
        Object.defineProperty(HTMLDialogElement.prototype, "close", {
            configurable: true,
            value(this: HTMLDialogElement) {
                this.removeAttribute("open");
                this.dispatchEvent(new Event("close"));
            }
        });
    });

    beforeEach(() => {
        Object.values(apiMocks).forEach((mock) => mock.mockReset());
        apiMocks.getBoard.mockResolvedValue(boardFixture);
        apiMocks.moveTask.mockResolvedValue(undefined);
        apiMocks.createTask.mockResolvedValue(undefined);
        apiMocks.updateTask.mockResolvedValue(undefined);
        apiMocks.deleteTask.mockResolvedValue(undefined);
        apiMocks.updateCapacity.mockResolvedValue(undefined);
    });

    it("faz o movimento de forma otimista e devolve o cartão se a API falhar", async () => {
        const user = userEvent.setup();
        let rejectMove: ((reason: Error) => void) | undefined;
        apiMocks.moveTask.mockReturnValue(
            new Promise<void>((_resolve, reject) => {
                rejectMove = reject;
            })
        );

        render(<BoardApp initialBoard={boardFixture} />);

        await user.click(screen.getByText("Mover"));
        await user.click(
            screen.getByRole("button", {
                name: "Mover Revisar API para Em andamento"
            })
        );

        expect(
            within(
                screen.getByRole("region", { name: "Em andamento" })
            ).getByText("Revisar API")
        ).toBeVisible();

        rejectMove?.(new Error("A API ficou indisponível."));

        expect(await screen.findByRole("alert")).toHaveTextContent(
            "A API ficou indisponível."
        );
        await waitFor(() => {
            expect(
                within(
                    screen.getByRole("region", { name: "A planejar" })
                ).getByText("Revisar API")
            ).toBeVisible();
        });
    });

    it("confirma o movimento e sincroniza o quadro retornado pela API", async () => {
        const user = userEvent.setup();
        const refreshed = moveTaskLocally(
            boardFixture,
            "cd53b05d-dad0-4436-bf61-7a926310540c",
            "thisWeek"
        );
        apiMocks.getBoard.mockResolvedValue(refreshed);
        render(<BoardApp initialBoard={boardFixture} />);

        await user.click(screen.getByText("Mover"));
        await user.click(
            screen.getByRole("button", {
                name: "Mover Revisar API para Esta semana"
            })
        );

        expect(apiMocks.moveTask).toHaveBeenCalledWith(
            "cd53b05d-dad0-4436-bf61-7a926310540c",
            "thisWeek"
        );
        expect(await screen.findByText(/Revisar API foi movida/)).toHaveAttribute(
            "role",
            "status"
        );
        expect(
            within(
                screen.getByRole("region", { name: "Esta semana" })
            ).getByText("Revisar API")
        ).toBeVisible();
    });

    it("mantém o movimento otimista quando a escrita foi salva e apenas o refetch falha", async () => {
        const user = userEvent.setup();
        apiMocks.getBoard.mockRejectedValueOnce(
            new Error("O snapshot não pôde ser atualizado.")
        );
        render(<BoardApp initialBoard={boardFixture} />);

        await user.click(screen.getByText("Mover"));
        await user.click(
            screen.getByRole("button", {
                name: "Mover Revisar API para Em andamento"
            })
        );

        expect(await screen.findByRole("alert")).toHaveTextContent(
            /alteração foi salva, mas não foi possível atualizar o quadro/i
        );
        expect(
            within(
                screen.getByRole("region", { name: "Em andamento" })
            ).getByText("Revisar API")
        ).toBeVisible();
        expect(
            within(
                screen.getByRole("region", { name: "A planejar" })
            ).queryByText("Revisar API")
        ).not.toBeInTheDocument();
    });

    it("restaura o foco no cartão depois do movimento pelo menu", async () => {
        const user = userEvent.setup();
        apiMocks.moveTask.mockReturnValue(new Promise<void>(() => undefined));
        render(<BoardApp initialBoard={boardFixture} />);

        await user.click(screen.getByText("Mover"));
        await user.click(
            screen.getByRole("button", {
                name: "Mover Revisar API para Esta semana"
            })
        );

        await waitFor(() =>
            expect(
                within(
                    screen.getByRole("region", { name: "Esta semana" })
                ).getByRole("article", { name: "Atividade Revisar API" })
            ).toHaveFocus()
        );
    });

    it("serializa as mutações e explica quando já existe uma gravação pendente", async () => {
        const user = userEvent.setup();
        apiMocks.moveTask.mockReturnValue(new Promise<void>(() => undefined));
        render(<BoardApp initialBoard={boardFixture} />);

        await user.click(screen.getByText("Mover"));
        await user.click(
            screen.getByRole("button", {
                name: "Mover Revisar API para Esta semana"
            })
        );

        expect(screen.getByRole("button", { name: "Editar" })).toBeDisabled();
        expect(screen.getByRole("button", { name: "Excluir" })).toBeDisabled();
        expect(
            screen.getByRole("button", { name: "Arrastar Revisar API" })
        ).toBeDisabled();
        const moveSummary = screen.getByText("Mover", { selector: "summary" });
        expect(moveSummary).toHaveAttribute("aria-disabled", "true");
        await user.click(moveSummary);
        expect(moveSummary.parentElement).not.toHaveAttribute("open");

        await user.type(screen.getByLabelText("Atividade"), "Outra atividade");
        await user.click(
            screen.getByRole("button", { name: "Adicionar ao quadro" })
        );

        expect(apiMocks.createTask).not.toHaveBeenCalled();
        expect(screen.getByRole("alert")).toHaveTextContent(
            /aguarde a alteração em andamento/i
        );
    });

    it("cadastra uma nova atividade e anuncia o sucesso", async () => {
        const user = userEvent.setup();
        render(<BoardApp initialBoard={boardFixture} />);

        await user.type(screen.getByLabelText("Atividade"), "Preparar entrevista");
        await user.click(
            screen.getByRole("button", { name: "Adicionar ao quadro" })
        );

        expect(apiMocks.createTask).toHaveBeenCalledWith(
            expect.objectContaining({
                title: "Preparar entrevista",
                dueDate: "2026-09-10",
                status: "planning"
            })
        );
        expect(await screen.findByText(/Atividade adicionada/)).toHaveAttribute(
            "role",
            "status"
        );
    });

    it("trata o cadastro como concluído se só a sincronização posterior falhar", async () => {
        const user = userEvent.setup();
        apiMocks.getBoard.mockRejectedValueOnce(
            new Error("O snapshot não pôde ser atualizado.")
        );
        render(<BoardApp initialBoard={boardFixture} />);

        const title = screen.getByLabelText("Atividade");
        await user.type(title, "Preparar entrevista");
        await user.click(
            screen.getByRole("button", { name: "Adicionar ao quadro" })
        );

        expect(apiMocks.createTask).toHaveBeenCalledTimes(1);
        expect(await screen.findByRole("alert")).toHaveTextContent(
            /alteração foi salva, mas não foi possível atualizar o quadro/i
        );
        expect(title).toHaveValue("");
    });

    it("mostra uma falha segura quando o cadastro não pode ser concluído", async () => {
        const user = userEvent.setup();
        apiMocks.createTask.mockRejectedValue(null);
        render(<BoardApp initialBoard={boardFixture} />);

        await user.type(screen.getByLabelText("Atividade"), "Preparar entrevista");
        await user.click(
            screen.getByRole("button", { name: "Adicionar ao quadro" })
        );

        expect(await screen.findByRole("alert")).toHaveTextContent(
            "Não foi possível concluir a ação."
        );
    });

    it("edita a atividade em um diálogo nativo", async () => {
        const user = userEvent.setup();
        render(<BoardApp initialBoard={boardFixture} />);

        await user.click(screen.getByRole("button", { name: "Editar" }));
        const dialog = screen.getByRole("dialog", { name: "Editar atividade" });
        const title = within(dialog).getByLabelText("Atividade");
        await user.clear(title);
        await user.type(title, "Revisar contrato da API");
        await user.click(
            within(dialog).getByRole("button", { name: "Salvar alterações" })
        );

        expect(apiMocks.updateTask).toHaveBeenCalledWith(
            "cd53b05d-dad0-4436-bf61-7a926310540c",
            expect.objectContaining({ title: "Revisar contrato da API" })
        );
        expect(await screen.findByText(/Atividade atualizada/)).toHaveAttribute(
            "role",
            "status"
        );
    });

    it("atualiza a capacidade e exclui uma atividade confirmada", async () => {
        const user = userEvent.setup();
        vi.spyOn(window, "confirm").mockReturnValue(true);
        render(<BoardApp initialBoard={boardFixture} />);

        const capacity = screen.getByLabelText("Ajustar");
        await user.clear(capacity);
        await user.type(capacity, "20");
        await user.click(screen.getByRole("button", { name: "Salvar" }));
        expect(apiMocks.updateCapacity).toHaveBeenCalledWith(20);

        await user.click(screen.getByRole("button", { name: "Excluir" }));
        expect(window.confirm).toHaveBeenCalledWith(
            "Excluir “Revisar API”? Esta ação não pode ser desfeita."
        );
        expect(apiMocks.deleteTask).toHaveBeenCalledWith(
            "cd53b05d-dad0-4436-bf61-7a926310540c"
        );
    });

    it("não exclui quando a confirmação é cancelada", async () => {
        const user = userEvent.setup();
        vi.spyOn(window, "confirm").mockReturnValue(false);
        render(<BoardApp initialBoard={boardFixture} />);

        await user.click(screen.getByRole("button", { name: "Excluir" }));

        expect(apiMocks.deleteTask).not.toHaveBeenCalled();
    });
});
