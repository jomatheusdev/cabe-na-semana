import { afterEach, describe, expect, it } from "vitest";

import { kanbanKeyboardCoordinates } from "../keyboard-coordinates";

type CoordinateArgs = Parameters<typeof kanbanKeyboardCoordinates>[1];

function argsFor(
    status: string | undefined,
    rectangles: Map<string, unknown> = new Map()
): CoordinateArgs {
    return {
        active: "task-id",
        currentCoordinates: { x: 10, y: 20 },
        context: {
            active: status
                ? { data: { current: { task: { status } } } }
                : null,
            over: null,
            collisionRect: { width: 200 },
            droppableRects: rectangles
        }
    } as unknown as CoordinateArgs;
}

describe("kanbanKeyboardCoordinates", () => {
    afterEach(() => {
        document.body.replaceChildren();
    });

    it("posiciona o cartão no centro da próxima coluna", () => {
        const event = new KeyboardEvent("keydown", {
            code: "ArrowRight",
            cancelable: true
        });
        const result = kanbanKeyboardCoordinates(
            event,
            argsFor(
                "planning",
                new Map([
                    [
                        "column:thisWeek",
                        { left: 300, top: 100, width: 280, height: 400 }
                    ]
                ])
            )
        );

        expect(result).toEqual({ x: 340, y: 100 });
        expect(event.defaultPrevented).toBe(true);
    });

    it("usa a coluna sobrevoada e também entende as setas verticais", () => {
        const event = new KeyboardEvent("keydown", { code: "ArrowUp" });
        const args = argsFor("planning", new Map());
        args.context.over = {
            data: { current: { status: "thisWeek" } }
        } as unknown as CoordinateArgs["context"]["over"];
        args.context.droppableRects.set(
            "column:planning",
            { left: 0, top: 50, width: 180, height: 300 } as never
        );

        expect(kanbanKeyboardCoordinates(event, args)).toEqual({ x: 0, y: 50 });
    });

    it("usa a geometria do DOM quando o dnd-kit ainda não mediu a coluna", () => {
        document.body.innerHTML =
            '<section data-kanban-status="thisWeek"></section>';
        const column = document.querySelector("section");
        expect(column).not.toBeNull();
        column!.getBoundingClientRect = () =>
            ({
                left: 420,
                top: 160,
                width: 320
            }) as DOMRect;

        expect(
            kanbanKeyboardCoordinates(
                new KeyboardEvent("keydown", { code: "ArrowRight" }),
                argsFor("planning", new Map())
            )
        ).toEqual({ x: 480, y: 160 });
    });

    it("mantém as coordenadas no limite ou sem geometria e ignora outras teclas", () => {
        const atBoundary = argsFor("planning");
        expect(
            kanbanKeyboardCoordinates(
                new KeyboardEvent("keydown", { code: "ArrowLeft" }),
                atBoundary
            )
        ).toEqual({ x: 10, y: 20 });
        expect(
            kanbanKeyboardCoordinates(
                new KeyboardEvent("keydown", { code: "ArrowRight" }),
                atBoundary
            )
        ).toEqual({ x: 10, y: 20 });
        expect(
            kanbanKeyboardCoordinates(
                new KeyboardEvent("keydown", { code: "Enter" }),
                atBoundary
            )
        ).toBeUndefined();
        expect(
            kanbanKeyboardCoordinates(
                new KeyboardEvent("keydown", { code: "ArrowRight" }),
                argsFor(undefined)
            )
        ).toEqual({ x: 10, y: 20 });
    });
});
