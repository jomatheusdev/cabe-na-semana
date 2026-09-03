import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

import { boardFixture } from "@/tests/fixtures/board";

import { KanbanBoard } from "../KanbanBoard";
import { moveTaskLocally } from "../board-state";

describe("KanbanBoard", () => {
    it("oferece um botão acessível como alternativa ao arraste", async () => {
        const user = userEvent.setup();
        const onMove = vi.fn().mockResolvedValue(undefined);

        render(
            <KanbanBoard
                board={boardFixture}
                onDelete={vi.fn()}
                onEdit={vi.fn()}
                onMove={onMove}
            />
        );

        expect(
            screen.getByRole("button", { name: "Arrastar Revisar API" })
        ).toHaveAttribute("aria-describedby");

        await user.click(screen.getByText("Mover"));
        await user.click(
            screen.getByRole("button", {
                name: "Mover Revisar API para Esta semana"
            })
        );

        expect(onMove).toHaveBeenCalledWith(
            "cd53b05d-dad0-4436-bf61-7a926310540c",
            "thisWeek",
            true
        );
    });

    it("move o cartão para a coluna de destino sem alterar o snapshot original", () => {
        const moved = moveTaskLocally(
            boardFixture,
            "cd53b05d-dad0-4436-bf61-7a926310540c",
            "inProgress"
        );

        expect(boardFixture.columns[0].tasks).toHaveLength(1);
        expect(moved.columns[0].tasks).toHaveLength(0);
        expect(moved.columns[2].tasks).toEqual([
            expect.objectContaining({
                title: "Revisar API",
                status: "inProgress"
            })
        ]);
    });
});
