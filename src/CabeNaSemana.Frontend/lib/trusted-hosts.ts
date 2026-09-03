const localHosts = ["localhost", "127.0.0.1", "::1"] as const;

function parseHostname(host: string): string | null {
    if (host.trim() !== host || host.length === 0) {
        return null;
    }

    try {
        const parsed = new URL(`http://${host}`);
        if (
            parsed.username ||
            parsed.password ||
            parsed.pathname !== "/" ||
            parsed.search ||
            parsed.hash
        ) {
            return null;
        }

        return parsed.hostname
            .replace(/^\[/, "")
            .replace(/\]$/, "")
            .replace(/\.$/, "")
            .toLowerCase();
    } catch {
        return null;
    }
}

export function isTrustedHost(
    host: string | null,
    configuredHosts = ""
): boolean {
    if (!host) {
        return false;
    }

    const hostname = parseHostname(host);
    if (!hostname) {
        return false;
    }

    const allowedHosts = new Set<string>(localHosts);
    for (const configuredHost of configuredHosts.split(",")) {
        const configuredHostname = parseHostname(configuredHost.trim());
        if (configuredHostname) {
            allowedHosts.add(configuredHostname);
        }
    }

    return allowedHosts.has(hostname);
}
