import { createServer } from "node:http";

const statuses = ["planning", "thisWeek", "inProgress", "completed"];
let taskStatus = "planning";

const task = {
    id: "cd53b05d-dad0-4436-bf61-7a926310540c",
    title: "Revisar API",
    dueDate: "2026-09-08",
    importance: "high",
    estimatedHours: 3,
    priorityScore: 74,
    priorityLevel: "high",
    priorityExplanation: "Alta importância e prazo próximo.",
    daysUntilDue: 5,
    capacityFit: "notScheduled"
};

function board() {
    return {
        today: "2026-09-03",
        totalTasks: 1,
        completedTasks: taskStatus === "completed" ? 1 : 0,
        capacity: {
            weeklyHours: 15,
            usedHours: taskStatus === "thisWeek" ? 3 : 0,
            availableHours: taskStatus === "thisWeek" ? 12 : 15,
            overbookedHours: 0,
            usagePercentage: taskStatus === "thisWeek" ? 20 : 0,
            isOverloaded: false
        },
        columns: statuses.map((status) => ({
            status,
            tasks: status === taskStatus ? [{ ...task, status }] : []
        }))
    };
}

function json(response, status, value) {
    response.writeHead(status, { "content-type": "application/json" });
    response.end(JSON.stringify(value));
}

async function readJson(request) {
    const chunks = [];
    for await (const chunk of request) {
        chunks.push(chunk);
    }
    return JSON.parse(Buffer.concat(chunks).toString("utf8"));
}

const server = createServer(async (request, response) => {
    const url = new URL(request.url ?? "/", "http://127.0.0.1:4301");

    if (request.method === "POST" && url.pathname === "/__reset") {
        taskStatus = "planning";
        response.writeHead(204);
        response.end();
        return;
    }

    if (request.method === "GET" && url.pathname === "/api/board") {
        json(response, 200, board());
        return;
    }

    if (
        request.method === "PATCH" &&
        url.pathname === `/api/tasks/${task.id}/status`
    ) {
        const body = await readJson(request);
        if (!statuses.includes(body.status)) {
            json(response, 400, { title: "Status inválido.", status: 400 });
            return;
        }
        taskStatus = body.status;
        response.writeHead(204);
        response.end();
        return;
    }

    json(response, 404, { title: "Recurso não encontrado.", status: 404 });
});

server.listen(4301, "127.0.0.1");
