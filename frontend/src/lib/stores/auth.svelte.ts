const TOKEN_KEY = 'auth_token';

interface JwtPayload {
    sub: string;
    name: string;
    role: string;
    exp: number;
}

function parseJwtPayload(token: string): JwtPayload | null {
    try {
        const parts = token.split('.');
        if (parts.length !== 3) return null;
        const payload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
        const json = atob(payload);
        return JSON.parse(json) as JwtPayload;
    } catch {
        return null;
    }
}

function isTokenExpired(payload: JwtPayload): boolean {
    return Date.now() / 1000 >= payload.exp;
}

function createAuthStore() {
    let _token: string | null = null;
    let _payload: JwtPayload | null = null;

    if (typeof localStorage !== 'undefined') {
        const stored = localStorage.getItem(TOKEN_KEY);
        if (stored) {
            const parsed = parseJwtPayload(stored);
            if (parsed && !isTokenExpired(parsed)) {
                _token = stored;
                _payload = parsed;
            } else {
                localStorage.removeItem(TOKEN_KEY);
            }
        }
    }

    let token = $state<string | null>(_token);
    let payload = $state<JwtPayload | null>(_payload);

    return {
        get token() { return token; },
        get isLoggedIn() { return token !== null && payload !== null; },
        get role() { return payload?.role ?? null; },
        get name() { return payload?.name ?? null; },
        get userId() { return payload ? parseInt(payload.sub) : null; },

        login(newToken: string): boolean {
            const parsed = parseJwtPayload(newToken);
            if (!parsed || isTokenExpired(parsed)) return false;
            token = newToken;
            payload = parsed;
            localStorage.setItem(TOKEN_KEY, newToken);
            return true;
        },

        logout() {
            token = null;
            payload = null;
            localStorage.removeItem(TOKEN_KEY);
        }
    };
}

export const auth = createAuthStore();
