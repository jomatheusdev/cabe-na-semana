import type { NextConfig } from "next";

const apiInternalUrl = (
    process.env.API_INTERNAL_URL ?? "http://localhost:5147"
).replace(/\/$/, "");

const contentSecurityPolicy = [
    "default-src 'self'",
    "base-uri 'self'",
    "object-src 'none'",
    "frame-ancestors 'none'",
    "form-action 'self'",
    "img-src 'self' data:",
    "font-src 'self'",
    "style-src 'self' 'unsafe-inline'",
    `script-src 'self' 'unsafe-inline'${
        process.env.NODE_ENV === "development" ? " 'unsafe-eval'" : ""
    }`,
    "connect-src 'self'"
].join("; ");

const nextConfig: NextConfig = {
    output: "standalone",
    poweredByHeader: false,
    async headers() {
        return [
            {
                source: "/:path*",
                headers: [
                    { key: "Content-Security-Policy", value: contentSecurityPolicy },
                    { key: "X-Frame-Options", value: "DENY" },
                    { key: "X-Content-Type-Options", value: "nosniff" },
                    {
                        key: "Referrer-Policy",
                        value: "strict-origin-when-cross-origin"
                    },
                    {
                        key: "Permissions-Policy",
                        value: "camera=(), microphone=(), geolocation=()"
                    }
                ]
            }
        ];
    },
    async rewrites() {
        return [
            {
                source: "/api/:path*",
                destination: `${apiInternalUrl}/api/:path*`
            }
        ];
    }
};

export default nextConfig;
