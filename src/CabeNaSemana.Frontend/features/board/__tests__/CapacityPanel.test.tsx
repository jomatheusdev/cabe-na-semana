import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";

import { boardFixture } from "@/tests/fixtures/board";

import { CapacityPanel } from "../CapacityPanel";

describe("CapacityPanel", () => {
    it("rejeita um limite fora do intervalo e envia um valor válido", async () => {
        const user = userEvent.setup();
        const onSave = vi.fn().mockResolvedValue(true);
        render(
            <CapacityPanel capacity={boardFixture.capacity} onSave={onSave} />
        );
        const input = screen.getByLabelText("Ajustar");

        await user.clear(input);
        await user.type(input, "90");
        await user.click(screen.getByRole("button", { name: "Salvar" }));
        expect(onSave).not.toHaveBeenCalled();
        expect(screen.getByRole("alert")).toHaveTextContent(
            "Informe entre 1 e 80 horas."
        );

        await user.clear(input);
        await user.type(input, "20");
        await user.click(screen.getByRole("button", { name: "Salvar" }));
        expect(onSave).toHaveBeenCalledWith(20);
    });

    it("mostra o excesso quando a semana está sobrecarregada", () => {
        render(
            <CapacityPanel
                capacity={{
                    ...boardFixture.capacity,
                    isOverloaded: true,
                    overbookedHours: 2,
                    usagePercentage: 120
                }}
                busy
                onSave={vi.fn()}
            />
        );

        expect(screen.getByText(/Excesso de/)).toHaveTextContent("Excesso de 2 h");
        expect(screen.getByRole("progressbar")).toHaveAttribute(
            "aria-valuenow",
            "3"
        );
    });
});
