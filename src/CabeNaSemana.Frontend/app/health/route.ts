export function GET(): Response {
    return new Response("healthy", {
        status: 200,
        headers: { "content-type": "text/plain; charset=utf-8" }
    });
}
