import { describe, expect, it } from "vitest";

import { boardFixture } from "@/tests/fixtures/board";

import { moveTaskLocally } from "../board-state";
import { deadlineLabel } from "../labels";

describe("deadlineLabel", () => {
    it.each([
        ["completed", 5, true, "Entregue · 08/09"],
        ["overdue", -2, false, "2 dias em atraso"],
        ["overdue singular", -1, false, "1 dia em atraso"],
        ["today", 0, false, "Vence hoje"],
        ["tomorrow", 1, false, "Vence amanhã"],
        ["future", 5, false, "08/09 · faltam 5 dias"]
    ])("formata o prazo %s", (_name, days, completed, expected) => {
        expect(deadlineLabel("2026-09-08", days as number, completed as boolean)).toBe(
            expected
        );
    });
});

describe("moveTaskLocally", () => {
    it("mantém a mesma referência quando a tarefa não existe ou não muda", () => {
        expect(moveTaskLocally(boardFixture, "ausente", "thisWeek")).toBe(
            boardFixture
        );
        expect(
            moveTaskLocally(
                boardFixture,
                "cd53b05d-dad0-4436-bf61-7a926310540c",
                "planning"
            )
        ).toBe(boardFixture);
    });

    it("atualiza o total concluído nos dois sentidos", () => {
        const completed = moveTaskLocally(
            boardFixture,
            "cd53b05d-dad0-4436-bf61-7a926310540c",
            "completed"
        );
        const reopened = moveTaskLocally(
            completed,
            "cd53b05d-dad0-4436-bf61-7a926310540c",
            "planning"
        );

        expect(completed.completedTasks).toBe(1);
        expect(completed.columns[3].tasks[0].capacityFit).toBe("completed");
        expect(reopened.completedTasks).toBe(0);
    });
});
