import type { PageLoad } from './$types';

export const ssr = false; // Disable server-side rendering

export const load: PageLoad = async ({ fetch, url }) => {
  const token = url.searchParams.get('token');

  if (!token) {
    return { status: 'missing-token' };
  }

  const payload = {
    Token: token
  };

  const res = await fetch(`${import.meta.env.VITE_API_URL}/api/bookings/confirm`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(payload)
    });

  if (!res.ok) {
    return { status: res.status };
  }

  return { status: res.status };
};