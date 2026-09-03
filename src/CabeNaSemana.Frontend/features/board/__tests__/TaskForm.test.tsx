import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

import { TaskForm } from "../TaskForm";

describe("TaskForm", () => {
    it("explica o campo obrigatório e não envia um título vazio", async () => {
        const user = userEvent.setup();
        const onSubmit = vi.fn();

        render(
            <TaskForm
                defaultDueDate="2026-09-10"
                onSubmit={onSubmit}
                submitLabel="Adicionar ao quadro"
            />
        );

        await user.click(
            screen.getByRole("button", { name: "Adicionar ao quadro" })
        );

        expect(onSubmit).not.toHaveBeenCalled();
        expect(
            screen.getByText("Informe o título da atividade.")
        ).toHaveAttribute("role", "alert");
        expect(screen.getByLabelText("Atividade")).toHaveAttribute(
            "aria-invalid",
            "true"
        );
    });

    it("envia um comando tipado com os valores informados", async () => {
        const user = userEvent.setup();
        const onSubmit = vi.fn().mockResolvedValue(undefined);

        render(
            <TaskForm
                defaultDueDate="2026-09-10"
                onSubmit={onSubmit}
                submitLabel="Adicionar ao quadro"
            />
        );

        await user.type(screen.getByLabelText("Atividade"), "Preparar demo");
        await user.clear(screen.getByLabelText("Prazo"));
        await user.type(screen.getByLabelText("Prazo"), "2026-09-12");
        await user.selectOptions(screen.getByLabelText("Importância"), "critical");
        await user.clear(screen.getByLabelText("Esforço estimado"));
        await user.type(screen.getByLabelText("Esforço estimado"), "4.5");
        await user.selectOptions(screen.getByLabelText("Coluna"), "thisWeek");
        await user.click(
            screen.getByRole("button", { name: "Adicionar ao quadro" })
        );

        expect(onSubmit).toHaveBeenCalledWith({
            title: "Preparar demo",
            dueDate: "2026-09-12",
            importance: "critical",
            estimatedHours: 4.5,
            status: "thisWeek"
        });
    });
});
