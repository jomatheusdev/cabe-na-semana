import { type NextRequest, NextResponse } from "next/server";

import { isTrustedHost } from "./lib/trusted-hosts";

export function proxy(request: NextRequest): NextResponse {
    if (!isTrustedHost(request.headers.get("host"), process.env.APP_ALLOWED_HOSTS)) {
        return new NextResponse("Host não permitido.", {
            status: 400,
            headers: {
                "cache-control": "no-store",
                "content-type": "text/plain; charset=utf-8"
            }
        });
    }

    return NextResponse.next();
}

export const config = {
    matcher: "/:path*"
};
