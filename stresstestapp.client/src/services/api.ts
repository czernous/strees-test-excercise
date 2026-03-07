import type {
    CreateCalculationRequest,
    CreateCalculationResponse,
    ListCalculationsResponse,
    GetCalculationResponse,
    ListCountriesResponse
} from '../types';

// Health check - polls server until it's ready
async function checkServerHealth(maxAttempts = 30): Promise<boolean> {
    for (let attempt = 1; attempt <= maxAttempts; attempt++) {
        try {
            const controller = new AbortController();
            const timeoutId = setTimeout(() => controller.abort(), 2000);

            const response = await fetch('/health', {
                signal: controller.signal,
                cache: 'no-store'
            });

            clearTimeout(timeoutId);

            if (response.ok) {
                return true;
            }
        } catch {
            // Server not ready yet, will retry
        }

        // Wait before next attempt (exponential backoff, max 3s)
        const delay = Math.min(1000 * Math.pow(1.3, attempt - 1), 3000);
        await new Promise(resolve => setTimeout(resolve, delay));
    }

    return false;
}

export function waitForServerHealth(): Promise<boolean> {
    return fetchWithCache('server-health', checkServerHealth);
}

// Cache for data fetching with use()
const cache = new Map<string, Promise<unknown>>();
const abortControllers = new Map<string, AbortController>();

// Timeout wrapper for fetch with abort support
async function fetchWithTimeout(
    url: string,
    options: RequestInit = {},
    timeout = 10000
): Promise<Response> {
    const controller = new AbortController();
    const id = `${url}-${Date.now()}`;
    abortControllers.set(id, controller);

    const timeoutId = setTimeout(() => {
        controller.abort();
        abortControllers.delete(id);
    }, timeout);

    try {
        const response = await fetch(url, {
            ...options,
            signal: controller.signal,
        });
        clearTimeout(timeoutId);
        abortControllers.delete(id);
        return response;
    } catch (error) {
        clearTimeout(timeoutId);
        abortControllers.delete(id);
        if (error instanceof Error && error.name === 'AbortError') {
            throw new Error('Request timed out. Please check if the server is running.');
        }
        throw error;
    }
}

function fetchWithCache<T>(key: string, fetchFn: () => Promise<T>): Promise<T> {
    if (!cache.has(key)) {
        const promise = fetchFn()
            .catch((error) => {
                // Remove from cache on error to allow retry via ErrorBoundary
                cache.delete(key);
                throw error;
            });
        cache.set(key, promise);
    }
    return cache.get(key) as Promise<T>;
}

export function invalidateCache(pattern?: string) {
    if (pattern) {
        for (const key of cache.keys()) {
            if (key.includes(pattern)) {
                cache.delete(key);
            }
        }
    } else {
        cache.clear();
    }
}

export function getCountries(): Promise<ListCountriesResponse> {
    return fetchWithCache('countries', async () => {
        const response = await fetchWithTimeout('/api/countries');
        if (!response.ok) {
            throw new Error(`Failed to load countries: ${response.statusText}`);
        }
        return response.json();
    });
}

export function getCalculations(): Promise<ListCalculationsResponse> {
    return fetchWithCache('calculations', async () => {
        const response = await fetchWithTimeout('/api/calculations');
        if (!response.ok) {
            throw new Error(`Failed to load calculations: ${response.statusText}`);
        }
        return response.json();
    });
}

export function getCalculationById(id: string): Promise<GetCalculationResponse> {
    return fetchWithCache(`calculation-${id}`, async () => {
        const response = await fetchWithTimeout(`/api/calculations/${id}`);
        if (!response.ok) {
            throw new Error(`Failed to load calculation details: ${response.statusText}`);
        }
        return response.json();
    });
}

export async function createCalculation(request: CreateCalculationRequest): Promise<CreateCalculationResponse> {
    const response = await fetchWithTimeout(
        '/api/calculations',
        {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(request),
        },
        30000 // 30 second timeout for calculations
    );

    if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.detail || `Failed to create calculation: ${response.statusText}`);
    }

    invalidateCache('calculations');

    return response.json();
}

// Cancel all ongoing requests
export function cancelAllRequests() {
    for (const controller of abortControllers.values()) {
        controller.abort();
    }
    abortControllers.clear();
}
