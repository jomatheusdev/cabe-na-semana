import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
    testDir: "./tests/e2e",
    fullyParallel: false,
    forbidOnly: Boolean(process.env.CI),
    retries: process.env.CI ? 2 : 0,
    workers: 1,
    reporter: [["list"], ["html", { open: "never" }]],
    use: {
        baseURL: "http://127.0.0.1:3000",
        trace: "on-first-retry",
        screenshot: "only-on-failure",
        video: "retain-on-failure"
    },
    projects: [
        {
            name: "chromium",
            use: { ...devices["Desktop Chrome"] }
        }
    ],
    webServer: [
        {
            command: "node tests/e2e/mock-api.mjs",
            url: "http://127.0.0.1:4301/api/board",
            reuseExistingServer: false,
            timeout: 30_000
        },
        {
            command:
                "API_INTERNAL_URL=http://127.0.0.1:4301 npm run dev -- --hostname 127.0.0.1",
            url: "http://127.0.0.1:3000",
            reuseExistingServer: false,
            timeout: 120_000
        }
    ]
});
