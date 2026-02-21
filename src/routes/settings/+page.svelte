<script lang="ts">
    import { onMount } from 'svelte';
    import NavigationBar from '../NavigationBar.svelte';
    import Footer from '../Footer.svelte';
    import { auth } from '$lib/stores/auth';

    const API = import.meta.env.VITE_API_URL;

    let loading     = $state(true);
    let saving      = $state(false);
    let fetchError  = $state('');
    let successMsg  = $state('');

    let emailNotificationsEnabled = $state(true);
    let feedUrl     = $state('');
    let copyLabel   = $state('Copy URL');

    function authHeaders() {
        return { 'Content-Type': 'application/json', Authorization: `Bearer ${auth.token}` };
    }

    function webcalUrl(url: string) {
        return url.replace(/^https?/, 'webcal');
    }

    async function copyFeedUrl() {
        await navigator.clipboard.writeText(feedUrl);
        copyLabel = 'Copied!';
        setTimeout(() => { copyLabel = 'Copy URL'; }, 2000);
    }

    onMount(async () => {
        try {
            const [settingsRes, feedRes] = await Promise.all([
                fetch(`${API}/tech/settings`, { headers: authHeaders() }),
                fetch(`${API}/tech/calendar/feed-url`, { headers: authHeaders() })
            ]);
            if (!settingsRes.ok) { fetchError = 'Failed to load settings.'; return; }
            const data = await settingsRes.json();
            emailNotificationsEnabled = data.emailNotificationsEnabled;
            if (feedRes.ok) {
                const feedData = await feedRes.json();
                feedUrl = feedData.feedUrl;
            }
        } catch {
            fetchError = 'Network error.';
        } finally {
            loading = false;
        }
    });

    async function saveSettings() {
        saving = true; successMsg = ''; fetchError = '';
        try {
            const res = await fetch(`${API}/tech/settings`, {
                method: 'PUT',
                headers: authHeaders(),
                body: JSON.stringify({ emailNotificationsEnabled })
            });
            if (!res.ok) {
                fetchError = 'Failed to save settings.';
            } else {
                successMsg = 'Settings saved.';
            }
        } catch {
            fetchError = 'Network error.';
        } finally {
            saving = false;
        }
    }
</script>

<NavigationBar />

<main class="settings-page">
    <div class="settings-panel">

        <a href="/calendar" class="back-link">← Back to Calendar</a>

        <h1 class="page-title">Settings</h1>

        {#if loading}
            <p class="state-msg">Loading...</p>

        {:else if fetchError && !successMsg}
            <p class="state-msg error">{fetchError}</p>

        {:else}
            {#if successMsg}<div class="banner banner-success">{successMsg}</div>{/if}
            {#if fetchError}<div class="banner banner-error">{fetchError}</div>{/if}

            <section class="settings-section">
                <h2 class="section-heading">Email Notifications</h2>
                <p class="section-desc">
                    Receive an email when an appointment is added to, modified on, or cancelled from your schedule.
                    Emails contain only appointment date, time, and service — no client personal information.
                </p>

                <label class="toggle-label">
                    <input type="checkbox" bind:checked={emailNotificationsEnabled} class="toggle-input" />
                    <span class="toggle-track">
                        <span class="toggle-thumb"></span>
                    </span>
                    Email me when an appointment is booked, modified, or cancelled
                </label>
            </section>

            <section class="settings-section">
                <h2 class="section-heading">Apple Calendar</h2>
                <p class="section-desc">
                    Subscribe to a live feed of your confirmed appointments in Apple Calendar.
                    New bookings and cancellations will appear automatically.
                </p>
                {#if feedUrl}
                    <div class="calendar-actions">
                        <a class="btn btn-primary" href={webcalUrl(feedUrl)}>
                            Subscribe in Apple Calendar
                        </a>
                        <button class="btn btn-secondary" onclick={copyFeedUrl}>
                            {copyLabel}
                        </button>
                    </div>
                    <p class="feed-hint">
                        Use "Subscribe in Apple Calendar" on this device, or copy the URL to add it manually on another device.
                    </p>
                {:else}
                    <p class="section-desc">Feed URL unavailable.</p>
                {/if}
            </section>

            <div class="actions">
                <button class="btn btn-primary" onclick={saveSettings} disabled={saving}>
                    {saving ? 'Saving…' : 'Save Changes'}
                </button>
            </div>
        {/if}

    </div>
</main>

<Footer />

<style>
    .settings-page {
        min-height: calc(100vh - var(--nav-height));
        margin-top: var(--nav-height);
        background: var(--beige-color, #f5f0e8);
        padding: 2rem 1.5rem;
        display: flex;
        justify-content: center;
        align-items: flex-start;
    }

    .settings-panel {
        background: var(--white-color, #fff);
        border-radius: 12px;
        padding: 2rem;
        width: 100%;
        max-width: 700px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.08);
    }

    .back-link {
        display: inline-block;
        font-size: 0.875rem;
        color: var(--indigo-color, #5e6c7a);
        text-decoration: none;
        margin-bottom: 1.5rem;
    }

    .back-link:hover { text-decoration: underline; }

    .page-title {
        font-size: 1.3rem;
        font-weight: 700;
        text-align: left;
        text-transform: none;
        margin: 0 0 1.5rem 0;
        color: var(--black-color, #3f3f44);
    }

    .state-msg { color: #888; font-size: 0.95rem; text-align: center; padding: 2rem 0; }
    .state-msg.error { color: #c0392b; }

    .banner {
        border-radius: 8px;
        padding: 0.75rem 1rem;
        margin-bottom: 1rem;
        font-size: 0.9rem;
    }

    .banner-success { background: #d4edda; color: #2e7d32; }
    .banner-error   { background: #fde8e8; color: #c0392b; }

    .settings-section {
        margin-bottom: 1.75rem;
        padding-bottom: 1.75rem;
        border-bottom: 1px solid #eee;
    }

    .section-heading {
        font-size: 0.8rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: #888;
        text-align: left;
        margin: 0 0 0.5rem 0;
    }

    .section-desc {
        font-size: 0.875rem;
        color: #666;
        margin: 0 0 1rem 0;
        line-height: 1.5;
    }

    .toggle-label {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        cursor: pointer;
        font-size: 0.9rem;
        font-weight: 500;
        color: var(--black-color, #3f3f44);
    }

    .toggle-input { display: none; }

    .toggle-track {
        display: inline-flex;
        width: 40px;
        height: 22px;
        border-radius: 11px;
        background: #ccc;
        padding: 2px;
        transition: background 0.2s;
        flex-shrink: 0;
    }

    .toggle-input:checked + .toggle-track { background: var(--green-color, #769a95); }

    .toggle-thumb {
        width: 18px;
        height: 18px;
        border-radius: 50%;
        background: #fff;
        transition: transform 0.2s;
    }

    .toggle-input:checked + .toggle-track .toggle-thumb { transform: translateX(18px); }

    .actions {
        display: flex;
        gap: 0.75rem;
        padding-top: 1rem;
    }

    .btn {
        padding: 0.55rem 1.25rem;
        border-radius: 8px;
        font-size: 0.875rem;
        font-weight: 600;
        font-family: inherit;
        cursor: pointer;
        border: none;
        transition: opacity 0.15s, transform 0.1s;
    }

    .btn:disabled { opacity: 0.6; cursor: not-allowed; }
    .btn:not(:disabled):active { transform: scale(0.97); }

    .btn-primary       { background: var(--green-color, #769a95); color: #fff; text-decoration: none; }
    .btn-primary:hover { opacity: 0.88; }

    .btn-secondary       { background: #f0f0f0; color: var(--black-color, #3f3f44); }
    .btn-secondary:hover { background: #e0e0e0; }

    .calendar-actions {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
        align-items: center;
    }

    .feed-hint {
        margin: 0.75rem 0 0 0;
        font-size: 0.8rem;
        color: #999;
    }
</style>
