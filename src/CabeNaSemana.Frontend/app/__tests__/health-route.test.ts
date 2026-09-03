import { describe, expect, it } from "vitest";

import { GET } from "../health/route";

describe("GET /health", () => {
    it("responde sem carregar o quadro nem depender da API", async () => {
        const response = GET();

        expect(response.status).toBe(200);
        expect(response.headers.get("content-type")).toContain("text/plain");
        await expect(response.text()).resolves.toBe("healthy");
    });
});
