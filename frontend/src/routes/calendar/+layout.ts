import { redirect } from '@sveltejs/kit';
import { auth } from '$lib/stores/auth';

// Disable SSR so this load function runs in the browser where localStorage is available
export const ssr = false;

export function load() {
    if (!auth.isLoggedIn || auth.role !== 'Technician') {
        throw redirect(303, '/login');
    }
}
