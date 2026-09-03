import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

test.beforeEach(async ({ request }) => {
    await request.post("http://127.0.0.1:4301/__reset");
});

test("arrasta um cartão para outra coluna e persiste o novo status", async ({
    page
}) => {
    await page.goto("/");

    const handle = page.getByRole("button", { name: "Arrastar Revisar API" });
    const destination = page.getByRole("region", { name: "Em andamento" });
    await handle.scrollIntoViewIfNeeded();
    const sourceBox = await handle.boundingBox();
    const destinationBox = await destination.boundingBox();

    expect(sourceBox).not.toBeNull();
    expect(destinationBox).not.toBeNull();

    const patchResponse = page.waitForResponse(
        (response) =>
            response.request().method() === "PATCH" &&
            response.url().endsWith(`/api/tasks/${
                "cd53b05d-dad0-4436-bf61-7a926310540c"
            }/status`)
    );

    await page.mouse.move(
        sourceBox!.x + sourceBox!.width / 2,
        sourceBox!.y + sourceBox!.height / 2
    );
    await page.mouse.down();
    await page.mouse.move(
        destinationBox!.x + destinationBox!.width / 2,
        destinationBox!.y + 130,
        { steps: 12 }
    );
    await page.mouse.up();

    const response = await patchResponse;
    expect(response.status()).toBe(204);
    expect(response.request().postDataJSON()).toEqual({ status: "inProgress" });
    await expect(destination.getByText("Revisar API")).toBeVisible();
    await expect(page.locator(".flash-success")).toContainText("foi movida");
});

test("move pelo controle alternativo usando somente o teclado", async ({ page }) => {
    await page.goto("/");

    const planning = page.getByRole("region", { name: "A planejar" });
    const summary = planning.locator("summary", { hasText: "Mover" });
    await summary.focus();
    await page.keyboard.press("Enter");

    const moveButton = page.getByRole("button", {
        name: "Mover Revisar API para Esta semana"
    });
    await moveButton.focus();
    await page.keyboard.press("Enter");

    const movedCard = page
        .getByRole("region", { name: "Esta semana" })
        .getByRole("article", { name: "Atividade Revisar API" });
    await expect(movedCard).toBeVisible();
    await expect(movedCard).toBeFocused();
});

test("arrasta com o sensor de teclado do Kanban", async ({ page }) => {
    await page.goto("/");

    const handle = page.getByRole("button", { name: "Arrastar Revisar API" });
    const destination = page.getByRole("region", { name: "Esta semana" });
    await handle.focus();
    await page.keyboard.press("Space");
    await expect(page.locator(".drag-overlay")).toBeVisible();

    const patchResponse = page.waitForResponse(
        (response) =>
            response.request().method() === "PATCH" &&
            response.url().endsWith(`/api/tasks/${
                "cd53b05d-dad0-4436-bf61-7a926310540c"
            }/status`)
    );

    await page.keyboard.press("ArrowRight");
    await expect(destination).toHaveClass(/\bis-drop-target\b/);
    await page.keyboard.press("Space");

    const response = await patchResponse;
    expect(response.status()).toBe(204);
    expect(response.request().postDataJSON()).toEqual({ status: "thisWeek" });
    await expect(destination.getByText("Revisar API")).toBeVisible();
});

test("não apresenta violações automáticas de acessibilidade sérias", async ({
    page
}) => {
    await page.goto("/");

    const results = await new AxeBuilder({ page }).analyze();
    const seriousViolations = results.violations.filter(
        (violation) =>
            violation.impact === "critical" || violation.impact === "serious"
    );

    expect(seriousViolations).toEqual([]);
});
