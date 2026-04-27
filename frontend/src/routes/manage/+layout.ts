import { redirect } from '@sveltejs/kit';
import { auth } from '$lib/stores/auth';

export const ssr = false;

export function load() {
    if (!auth.isLoggedIn || auth.role !== 'Technician') {
        throw redirect(303, '/login');
    }
}
