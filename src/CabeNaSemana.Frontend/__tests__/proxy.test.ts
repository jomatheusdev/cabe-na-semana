import { NextRequest } from "next/server";
import { describe, expect, it } from "vitest";

import { proxy } from "../proxy";

describe("proxy", () => {
    it("blocks requests whose Host header is not trusted", () => {
        const request = new NextRequest("http://attacker.example/", {
            headers: { host: "attacker.example" }
        });

        const response = proxy(request);

        expect(response.status).toBe(400);
    });

    it("lets loopback requests continue", () => {
        const request = new NextRequest("http://localhost:3000/health", {
            headers: { host: "localhost:3000" }
        });

        const response = proxy(request);

        expect(response.status).toBe(200);
        expect(response.headers.get("x-middleware-next")).toBe("1");
    });
});
