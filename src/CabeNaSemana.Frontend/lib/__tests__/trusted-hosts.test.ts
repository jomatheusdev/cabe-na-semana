import { describe, expect, it } from "vitest";

import { isTrustedHost } from "../trusted-hosts";

describe("isTrustedHost", () => {
    it.each([
        "localhost",
        "localhost:8080",
        "127.0.0.1",
        "127.0.0.1:8080",
        "[::1]:8080"
    ])("accepts the local host %s", (host) => {
        expect(isTrustedHost(host)).toBe(true);
    });

    it("accepts an explicitly configured deployment host", () => {
        expect(isTrustedHost("planner.example.com", "planner.example.com")).toBe(true);
    });

    it.each([
        "attacker.example",
        "localhost.attacker.example",
        "attacker.example@localhost",
        "localhost/path"
    ])("rejects the untrusted or malformed host %s", (host) => {
        expect(isTrustedHost(host)).toBe(false);
    });

    it("rejects a missing host header", () => {
        expect(isTrustedHost(null)).toBe(false);
    });
});
