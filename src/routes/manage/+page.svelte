<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import NavigationBar from '../NavigationBar.svelte';
    import Footer from '../Footer.svelte';
    import { auth } from '$lib/stores/auth';

    const API = import.meta.env.VITE_API_URL;
    const DAYS = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];

    // ── Types ────────────────────────────────────────────────────────────────
    interface DayTimeRangeEditDto  { day: string; beginTime: string; endTime: string; }
    interface ScheduleDetailDto    { id: number; effStartDate: string; effEndDate: string | null; outage: boolean; timeRanges: DayTimeRangeEditDto[]; }
    interface AppointmentDetailDto { id: number; date: string; time: string; serviceId: number; serviceName: string; serviceDuration: number; clientName: string; status: string; }
    interface ServiceOptionDto     { id: number; name: string; duration: number; }
    interface AppointmentDetailResponseDto { appointment: AppointmentDetailDto; availableServices: ServiceOptionDto[]; }
    interface OverlapInfoDto       { apptId: number; time: string; clientName: string; serviceName: string; }

    // ── Page context ─────────────────────────────────────────────────────────
    let itemType = $state<'schedule' | 'appointment' | null>(null);
    let itemId   = $state<number | null>(null);
    let isNew    = $state(false);

    // ── Shared state ─────────────────────────────────────────────────────────
    let loading      = $state(true);
    let saving       = $state(false);
    let fetchError   = $state('');
    let successMsg   = $state('');
    let confirmDeactivate = $state(false);
    let confirmCancel     = $state(false);

    // ── Schedule state ───────────────────────────────────────────────────────
    let schedule = $state<ScheduleDetailDto | null>(null);
    // editable copies
    let effStartDate = $state('');
    let effEndDate   = $state('');
    let outage       = $state(false);
    let timeRanges   = $state<DayTimeRangeEditDto[]>([]);
    // new-schedule-only fields
    let newScheduleId       = $state<number | null>(null);
    let scheduleCreateSuccess = $state(false);

    // ── Appointment state ─────────────────────────────────────────────────────
    let appt              = $state<AppointmentDetailDto | null>(null);
    let availableServices = $state<ServiceOptionDto[]>([]);
    let apptDate          = $state('');
    let apptTime          = $state('');
    let apptServiceId     = $state(0);
    let overlaps          = $state<OverlapInfoDto[]>([]);
    // new-appointment-only fields
    let newClientName   = $state('');
    let newClientEmail  = $state('');
    let newApptId       = $state<number | null>(null);
    let createSuccess   = $state(false);
    let createOverlaps  = $state<OverlapInfoDto[]>([]);

    // ── Auth header ───────────────────────────────────────────────────────────
    function authHeaders() {
        return { 'Content-Type': 'application/json', Authorization: `Bearer ${auth.token}` };
    }

    // ── Fetch on mount ────────────────────────────────────────────────────────
    onMount(async () => {
        const params = new URLSearchParams(window.location.search);
        const type = params.get('type');
        const id   = params.get('id');

        if (type !== 'schedule' && type !== 'appointment') {
            fetchError = 'Invalid URL parameters.';
            loading = false;
            return;
        }

        itemType = type;

        if (id === 'new') {
            isNew = true;
            const dateParam = params.get('date') ?? '';
            if (type === 'schedule') {
                effStartDate = dateParam;
            } else {
                apptDate = dateParam;
                await fetchAvailableServices();
            }
            loading = false;
            return;
        }

        if (!id || isNaN(Number(id))) {
            fetchError = 'Invalid URL parameters.';
            loading = false;
            return;
        }

        itemId = Number(id);
        if (itemType === 'schedule') await fetchSchedule();
        else await fetchAppointment();
    });

    async function fetchSchedule() {
        try {
            const res = await fetch(`${API}/tech/schedule/${itemId}`, { headers: authHeaders() });
            if (!res.ok) { fetchError = res.status === 404 ? 'Schedule not found.' : 'Failed to load schedule.'; return; }
            schedule = await res.json() as ScheduleDetailDto;
            effStartDate = schedule.effStartDate;
            effEndDate   = schedule.effEndDate ?? '';
            outage       = schedule.outage;
            timeRanges   = schedule.timeRanges.map(r => ({ ...r }));
        } catch { fetchError = 'Network error.'; }
        finally  { loading = false; }
    }

    async function fetchAppointment() {
        try {
            const res = await fetch(`${API}/tech/appointment/${itemId}`, { headers: authHeaders() });
            if (!res.ok) { fetchError = res.status === 404 ? 'Appointment not found.' : 'Failed to load appointment.'; return; }
            const data = await res.json() as AppointmentDetailResponseDto;
            appt              = data.appointment;
            availableServices = data.availableServices;
            apptDate          = appt.date;
            apptTime          = appt.time;
            apptServiceId     = appt.serviceId;
        } catch { fetchError = 'Network error.'; }
        finally  { loading = false; }
    }

    async function fetchAvailableServices() {
        try {
            const res = await fetch(`${API}/tech/services`, { headers: authHeaders() });
            if (!res.ok) { fetchError = 'Failed to load services.'; return; }
            availableServices = await res.json() as ServiceOptionDto[];
            if (availableServices.length > 0) apptServiceId = availableServices[0].id;
        } catch { fetchError = 'Network error.'; }
    }

    async function createSchedule() {
        saving = true; successMsg = ''; fetchError = '';
        try {
            const res = await fetch(`${API}/tech/schedule`, {
                method: 'POST',
                headers: authHeaders(),
                body: JSON.stringify({ effStartDate, effEndDate: effEndDate || null, outage, timeRanges })
            });
            if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                fetchError = (err as any).message ?? 'Failed to create schedule.';
            } else {
                const data = await res.json();
                newScheduleId = data.id;
                scheduleCreateSuccess = true;
            }
        } catch { fetchError = 'Network error.'; }
        finally  { saving = false; }
    }

    async function createAppointment(force = false) {
        saving = true; fetchError = ''; createOverlaps = [];
        try {
            const res = await fetch(`${API}/tech/appointment`, {
                method: 'POST',
                headers: authHeaders(),
                body: JSON.stringify({
                    date: apptDate,
                    time: apptTime,
                    serviceId: apptServiceId,
                    clientName: newClientName,
                    clientEmail: newClientEmail,
                    forceCreate: force
                })
            });
            if (res.status === 409) {
                const body = await res.json();
                createOverlaps = body.overlaps ?? [];
            } else if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                fetchError = (err as any).message ?? 'Failed to create appointment.';
            } else {
                const data = await res.json();
                newApptId = data.id;
                createSuccess = true;
            }
        } catch { fetchError = 'Network error.'; }
        finally  { saving = false; }
    }

    // ── Schedule actions ──────────────────────────────────────────────────────
    function addTimeRange() {
        timeRanges = [...timeRanges, { day: 'Monday', beginTime: '09:00', endTime: '17:00' }];
    }

    function removeTimeRange(index: number) {
        timeRanges = timeRanges.filter((_, i) => i !== index);
    }

    async function saveSchedule() {
        saving = true; successMsg = '';
        try {
            const res = await fetch(`${API}/tech/schedule/${itemId}`, {
                method: 'PUT',
                headers: authHeaders(),
                body: JSON.stringify({
                    effStartDate,
                    effEndDate: effEndDate || null,
                    outage,
                    timeRanges
                })
            });
            if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                fetchError = (err as any).message ?? 'Failed to save schedule.';
            } else {
                successMsg = 'Schedule saved.';
            }
        } catch { fetchError = 'Network error.'; }
        finally  { saving = false; }
    }

    async function deactivateSchedule() {
        saving = true;
        try {
            const res = await fetch(`${API}/tech/schedule/${itemId}`, {
                method: 'DELETE', headers: authHeaders()
            });
            if (res.ok) goto('/calendar');
            else fetchError = 'Failed to deactivate schedule.';
        } catch { fetchError = 'Network error.'; }
        finally  { saving = false; confirmDeactivate = false; }
    }

    // ── Appointment actions ────────────────────────────────────────────────────
    async function saveAppointment(force = false) {
        saving = true; successMsg = ''; overlaps = []; fetchError = '';
        try {
            const res = await fetch(`${API}/tech/appointment/${itemId}`, {
                method: 'PUT',
                headers: authHeaders(),
                body: JSON.stringify({
                    date: apptDate,
                    time: apptTime,
                    serviceId: apptServiceId,
                    forceUpdate: force
                })
            });

            if (res.status === 409) {
                const body = await res.json();
                overlaps = body.overlaps ?? [];
            } else if (!res.ok) {
                const err = await res.json().catch(() => ({}));
                fetchError = (err as any).message ?? 'Failed to save appointment.';
            } else {
                successMsg = 'Appointment saved.';
                overlaps = [];
            }
        } catch { fetchError = 'Network error.'; }
        finally  { saving = false; }
    }

    async function cancelAppointment() {
        saving = true;
        try {
            const res = await fetch(`${API}/tech/appointment/${itemId}`, {
                method: 'DELETE', headers: authHeaders()
            });
            if (res.ok) goto('/calendar');
            else fetchError = 'Failed to cancel appointment.';
        } catch { fetchError = 'Network error.'; }
        finally  { saving = false; confirmCancel = false; }
    }
</script>

<NavigationBar />

<main class="manage-page">
    <div class="manage-panel">

        <!-- Back link -->
        <a href="/calendar" class="back-link">
            ← Back to Calendar
        </a>

        {#if loading}
            <p class="state-msg">Loading...</p>

        {:else if fetchError && !schedule && !appt && !isNew}
            <p class="state-msg error">{fetchError}</p>

        {:else if itemType === 'schedule' && (schedule || isNew)}

            <!-- ── Schedule Editor ── -->
            <h1 class="page-title">{isNew ? 'New Schedule' : 'Edit Schedule'}</h1>

            {#if isNew && scheduleCreateSuccess}
                <div class="banner banner-success">Schedule created successfully!</div>
                <div class="actions">
                    <a href="/calendar" class="btn btn-ghost">Back to Calendar</a>
                </div>

            {:else}

            {#if successMsg}<div class="banner banner-success">{successMsg}</div>{/if}
            {#if fetchError}<div class="banner banner-error">{fetchError}</div>{/if}

            <div class="form-grid">
                <div class="field">
                    <label for="effStart">Effective Start Date</label>
                    <input id="effStart" type="date" bind:value={effStartDate} />
                </div>
                <div class="field">
                    <label for="effEnd">Effective End Date <span class="optional">(leave blank = ongoing)</span></label>
                    <div class="input-row">
                        <input id="effEnd" type="date" bind:value={effEndDate} />
                        {#if effEndDate}
                            <button type="button" class="clear-btn" onclick={() => effEndDate = ''}>Clear</button>
                        {/if}
                    </div>
                </div>
                <div class="field field-row">
                    <label class="toggle-label">
                        <input type="checkbox" bind:checked={outage} class="toggle-input" />
                        <span class="toggle-track">
                            <span class="toggle-thumb"></span>
                        </span>
                        Mark as outage (unavailable)
                    </label>
                </div>
            </div>

            <!-- Time Ranges -->
            <div class="ranges-section">
                <div class="ranges-header">
                    <span class="section-label">Time Ranges</span>
                    <button type="button" class="add-btn" onclick={addTimeRange}>＋ Add time range</button>
                </div>

                {#if timeRanges.length === 0}
                    <p class="empty-ranges">No time ranges — this schedule has no working hours.</p>
                {:else}
                    {#each timeRanges as range, i}
                        <div class="range-row">
                            <select bind:value={range.day} class="day-select">
                                {#each DAYS as d}
                                    <option value={d}>{d}</option>
                                {/each}
                            </select>
                            <input type="time" bind:value={range.beginTime} class="time-input" />
                            <span class="range-sep">–</span>
                            <input type="time" bind:value={range.endTime} class="time-input" />
                            <button type="button" class="remove-btn" onclick={() => removeTimeRange(i)} aria-label="Remove">×</button>
                        </div>
                    {/each}
                {/if}
            </div>

            <!-- Actions -->
            <div class="actions">
                <button class="btn btn-primary" onclick={isNew ? createSchedule : saveSchedule} disabled={saving}>
                    {saving ? 'Saving…' : isNew ? 'Create Schedule' : 'Save Changes'}
                </button>

                {#if !isNew}
                    {#if !confirmDeactivate}
                        <button class="btn btn-danger-outline" onclick={() => confirmDeactivate = true}>
                            Deactivate Schedule
                        </button>
                    {:else}
                        <div class="confirm-inline">
                            <span>End this schedule today. Are you sure?</span>
                            <button class="btn btn-danger" onclick={deactivateSchedule} disabled={saving}>Yes, deactivate</button>
                            <button class="btn btn-ghost" onclick={() => confirmDeactivate = false}>Cancel</button>
                        </div>
                    {/if}
                {/if}
            </div>

            {/if}<!-- end isNew && scheduleCreateSuccess else -->

        {:else if itemType === 'appointment' && (appt || isNew)}

            <!-- ── Appointment Editor ── -->
            <h1 class="page-title">{isNew ? 'New Appointment' : 'Edit Appointment'}</h1>

            {#if isNew && createSuccess}
                <div class="banner banner-success">Appointment created successfully!</div>
                <div class="actions">
                    <a href="/calendar" class="btn btn-ghost">Back to Calendar</a>
                </div>

            {:else}

            <!-- Read-only info (existing appointments only) -->
            {#if !isNew && appt}
                <div class="info-bar">
                    <span class="info-client">{appt.clientName}</span>
                    <span
                        class="badge"
                        class:badge-confirmed={appt.status === 'confirmed'}
                        class:badge-pending={appt.status === 'pending'}
                        class:badge-cancelled={appt.status === 'cancelled'}
                    >{appt.status}</span>
                </div>
            {/if}

            {#if successMsg}<div class="banner banner-success">{successMsg}</div>{/if}
            {#if fetchError}<div class="banner banner-error">{fetchError}</div>{/if}

            <!-- Overlap warning (new appointment) -->
            {#if createOverlaps.length > 0 && isNew}
                <div class="banner banner-overlap">
                    <strong>Scheduling conflict detected:</strong>
                    <ul>
                        {#each createOverlaps as o}
                            <li>{o.time} — {o.clientName} ({o.serviceName})</li>
                        {/each}
                    </ul>
                    <button class="btn btn-warning" onclick={() => createAppointment(true)} disabled={saving}>
                        Create anyway
                    </button>
                </div>
            {/if}

            <!-- Overlap warning (edit mode only) -->
            {#if overlaps.length > 0 && !isNew}
                <div class="banner banner-overlap">
                    <strong>Scheduling conflict detected:</strong>
                    <ul>
                        {#each overlaps as o}
                            <li>{o.time} — {o.clientName} ({o.serviceName})</li>
                        {/each}
                    </ul>
                    <button class="btn btn-warning" onclick={() => saveAppointment(true)} disabled={saving}>
                        Save anyway
                    </button>
                </div>
            {/if}

            <div class="form-grid">
                {#if isNew}
                    <div class="field">
                        <label for="clientName">Client Name</label>
                        <input id="clientName" type="text" bind:value={newClientName} placeholder="Full name" />
                    </div>
                    <div class="field">
                        <label for="clientEmail">Client Email</label>
                        <input id="clientEmail" type="email" bind:value={newClientEmail} placeholder="email@example.com" />
                    </div>
                {/if}
                <div class="field">
                    <label for="apptDate">Date</label>
                    <input id="apptDate" type="date" bind:value={apptDate} />
                </div>
                <div class="field">
                    <label for="apptTime">Time</label>
                    <input id="apptTime" type="time" bind:value={apptTime} />
                </div>
                <div class="field">
                    <label for="apptService">Service</label>
                    <select id="apptService" bind:value={apptServiceId} class="service-select">
                        {#each availableServices as svc}
                            <option value={svc.id}>{svc.name} ({svc.duration} min)</option>
                        {/each}
                    </select>
                </div>
            </div>

            <!-- Actions -->
            <div class="actions">
                <button class="btn btn-primary" onclick={() => isNew ? createAppointment() : saveAppointment(false)} disabled={saving}>
                    {saving ? 'Saving…' : isNew ? 'Create Appointment' : 'Save Changes'}
                </button>

                {#if !isNew}
                    {#if !confirmCancel}
                        <button class="btn btn-danger-outline" onclick={() => confirmCancel = true}>
                            Cancel Appointment
                        </button>
                    {:else}
                        <div class="confirm-inline">
                            <span>Cancel this appointment? This cannot be undone.</span>
                            <button class="btn btn-danger" onclick={cancelAppointment} disabled={saving}>Yes, cancel it</button>
                            <button class="btn btn-ghost" onclick={() => confirmCancel = false}>Go back</button>
                        </div>
                    {/if}
                {/if}
            </div>

            {/if}<!-- end isNew && createSuccess else -->

        {/if}
    </div>
</main>

<Footer />

<style>
    .manage-page {
        min-height: calc(100vh - var(--nav-height));
        margin-top: var(--nav-height);
        background: var(--beige-color, #f5f0e8);
        padding: 2rem 1.5rem;
        display: flex;
        justify-content: center;
        align-items: flex-start;
    }

    .manage-panel {
        background: var(--white-color, #fff);
        border-radius: 12px;
        padding: 2rem;
        width: 100%;
        max-width: 700px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.08);
    }

    /* Back link */
    .back-link {
        display: inline-block;
        font-size: 0.875rem;
        color: var(--indigo-color, #5e6c7a);
        text-decoration: none;
        margin-bottom: 1.5rem;
    }

    .back-link:hover { text-decoration: underline; }

    /* Title */
    .page-title {
        font-size: 1.3rem;
        font-weight: 700;
        text-align: left;
        text-transform: none;
        margin: 0 0 1.5rem 0;
        color: var(--black-color, #3f3f44);
    }

    /* Info bar (appointment) */
    .info-bar {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        margin-bottom: 1.25rem;
    }

    .info-client {
        font-weight: 600;
        font-size: 1rem;
    }

    /* State messages */
    .state-msg { color: #888; font-size: 0.95rem; text-align: center; padding: 2rem 0; }
    .state-msg.error { color: #c0392b; }

    /* Banners */
    .banner {
        border-radius: 8px;
        padding: 0.75rem 1rem;
        margin-bottom: 1rem;
        font-size: 0.9rem;
    }

    .banner-success { background: #d4edda; color: #2e7d32; }
    .banner-error   { background: #fde8e8; color: #c0392b; }
    .banner-overlap {
        background: var(--warm-pink-color, #fbcdc1);
        color: var(--black-color, #3f3f44);
    }

    .banner-overlap ul {
        margin: 0.4rem 0 0.75rem 1.25rem;
        padding: 0;
        font-size: 0.88rem;
    }

    /* Form grid */
    .form-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 1.25rem;
        margin-bottom: 1.5rem;
    }

    .field { display: flex; flex-direction: column; gap: 0.35rem; }
    .field.field-row { grid-column: 1 / -1; }

    label {
        font-size: 0.8rem;
        font-weight: 600;
        color: #666;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        text-align: left;
    }

    .optional { font-weight: 400; text-transform: none; letter-spacing: 0; }

    input[type="date"],
    input[type="time"],
    input[type="text"],
    input[type="email"],
    .service-select,
    .day-select {
        padding: 0.5rem 0.65rem;
        border: 1px solid #ddd;
        border-radius: 6px;
        font-size: 0.9rem;
        font-family: inherit;
        background: var(--white-color, #fff);
        color: var(--black-color, #3f3f44);
        width: 100%;
    }

    input[type="date"]:focus,
    input[type="time"]:focus,
    input[type="text"]:focus,
    input[type="email"]:focus,
    .service-select:focus,
    .day-select:focus {
        outline: none;
        border-color: var(--pink-color, #e5b8c9);
    }

    .input-row { display: flex; gap: 0.5rem; align-items: center; }

    .clear-btn {
        font-size: 0.78rem;
        color: var(--indigo-color, #5e6c7a);
        background: none;
        border: none;
        cursor: pointer;
        padding: 0;
        white-space: nowrap;
    }

    .clear-btn:hover { text-decoration: underline; }

    /* Toggle */
    .toggle-label {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        cursor: pointer;
        font-size: 0.9rem;
        font-weight: 500;
        text-transform: none;
        letter-spacing: 0;
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

    /* Time ranges */
    .ranges-section { margin-bottom: 1.75rem; }

    .ranges-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 0.75rem;
        padding-bottom: 0.4rem;
        border-bottom: 1px solid #eee;
    }

    .section-label {
        font-size: 0.8rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: #888;
    }

    .add-btn {
        font-size: 0.82rem;
        color: var(--green-color, #769a95);
        background: none;
        border: 1px solid var(--green-color, #769a95);
        border-radius: 20px;
        padding: 2px 10px;
        cursor: pointer;
        font-family: inherit;
    }

    .add-btn:hover { background: var(--light-green-color, #bfd6d3); }

    .empty-ranges { font-size: 0.9rem; color: #aaa; margin: 0; }

    .range-row {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 0.5rem;
        flex-wrap: wrap;
    }

    .day-select  { flex: 1; min-width: 120px; max-width: 160px; }
    .time-input  { width: 110px; flex-shrink: 0; }
    .range-sep   { color: #888; flex-shrink: 0; }

    .remove-btn {
        background: none;
        border: none;
        color: #c0392b;
        font-size: 1.2rem;
        cursor: pointer;
        padding: 0 0.2rem;
        line-height: 1;
        flex-shrink: 0;
    }

    .remove-btn:hover { color: #922b21; }

    /* Actions */
    .actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
        align-items: center;
        padding-top: 1rem;
        border-top: 1px solid #eee;
    }

    /* Inline confirmation */
    .confirm-inline {
        display: flex;
        align-items: center;
        gap: 0.6rem;
        flex-wrap: wrap;
        background: #fff3cd;
        border-radius: 8px;
        padding: 0.6rem 0.9rem;
        font-size: 0.88rem;
        width: 100%;
    }

    /* Buttons */
    a.btn { text-decoration: none; display: inline-flex; align-items: center; }

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

    .btn-primary       { background: var(--green-color, #769a95); color: #fff; }
    .btn-primary:hover { opacity: 0.88; }

    .btn-danger-outline {
        background: transparent;
        border: 1.5px solid #c0392b;
        color: #c0392b;
    }
    .btn-danger-outline:hover { background: #fde8e8; }

    .btn-danger       { background: #c0392b; color: #fff; }
    .btn-danger:hover { opacity: 0.88; }

    .btn-warning       { background: var(--pink-color, #e5b8c9); color: var(--black-color, #3f3f44); }
    .btn-warning:hover { opacity: 0.88; }

    .btn-ghost       { background: none; color: #666; padding-left: 0.5rem; padding-right: 0.5rem; }
    .btn-ghost:hover { color: var(--black-color, #3f3f44); }

    /* Badges */
    .badge {
        font-size: 0.72rem;
        font-weight: 700;
        text-transform: uppercase;
        padding: 2px 8px;
        border-radius: 20px;
        letter-spacing: 0.04em;
    }

    .badge-confirmed  { background: var(--green-color, #769a95); color: #fff; }
    .badge-pending    { background: var(--warm-pink-color, #fbcdc1); color: var(--black-color, #3f3f44); }
    .badge-cancelled  { background: #ddd; color: #666; }

    /* Responsive */
    @media (max-width: 600px) {
        .form-grid { grid-template-columns: 1fr; }
        .day-select { max-width: 100%; }
        .time-input { width: 100%; }
    }
</style>
