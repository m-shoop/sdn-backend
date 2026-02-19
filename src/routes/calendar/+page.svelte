<script lang="ts">
    import { onMount } from 'svelte';
    import { goto } from '$app/navigation';
    import NavigationBar from '../NavigationBar.svelte';
    import Footer from '../Footer.svelte';
    import { auth } from '$lib/stores/auth';

    // ── Types ────────────────────────────────────────────────────────────────
    interface DayTimeRangeDto { beginTime: string; endTime: string; }
    interface ScheduleSummaryDto {
        id: number;
        effStartDate: string;
        effEndDate: string | null;
        outage: boolean;
        timeRangesForDay: DayTimeRangeDto[];
    }
    interface AppointmentSummaryDto {
        id: number;
        date: string;
        time: string;
        serviceName: string;
        serviceDuration: number;
        clientName: string;
        status: string;
    }
    interface CalendarDayResponse {
        schedules: ScheduleSummaryDto[];
        appointments: AppointmentSummaryDto[];
    }

    // ── Calendar state ───────────────────────────────────────────────────────
    const today = new Date();
    let viewYear = $state(today.getFullYear());
    let viewMonth = $state(today.getMonth()); // 0-indexed

    let selectedYear = $state(today.getFullYear());
    let selectedMonth = $state(today.getMonth());
    let selectedDay = $state(today.getDate());

    // ── Day panel state ──────────────────────────────────────────────────────
    let loading = $state(false);
    let dayData = $state<CalendarDayResponse | null>(null);
    let fetchError = $state('');

    // ── Calendar grid helpers ────────────────────────────────────────────────
    const MONTH_NAMES = [
        'January','February','March','April','May','June',
        'July','August','September','October','November','December'
    ];
    const DAY_NAMES = ['Sun','Mon','Tue','Wed','Thu','Fri','Sat'];

    interface CalendarCell { day: number | null; }

    function buildCalendarCells(year: number, month: number): CalendarCell[] {
        const firstDow = new Date(year, month, 1).getDay(); // 0=Sun
        const daysInMonth = new Date(year, month + 1, 0).getDate();
        const cells: CalendarCell[] = [];
        for (let i = 0; i < firstDow; i++) cells.push({ day: null });
        for (let d = 1; d <= daysInMonth; d++) cells.push({ day: d });
        // Pad to full 7-column rows
        while (cells.length % 7 !== 0) cells.push({ day: null });
        return cells;
    }

    let cells = $derived(buildCalendarCells(viewYear, viewMonth));

    function prevMonth() {
        if (viewMonth === 0) { viewMonth = 11; viewYear--; }
        else viewMonth--;
    }

    function nextMonth() {
        if (viewMonth === 11) { viewMonth = 0; viewYear++; }
        else viewMonth++;
    }

    function isToday(day: number) {
        return day === today.getDate() && viewMonth === today.getMonth() && viewYear === today.getFullYear();
    }

    function isSelected(day: number) {
        return day === selectedDay && viewMonth === selectedMonth && viewYear === selectedYear;
    }

    async function selectDay(day: number) {
        selectedDay = day;
        selectedMonth = viewMonth;
        selectedYear = viewYear;
        await fetchDayData();
    }

    // ── API fetch ────────────────────────────────────────────────────────────
    function selectedDateString() {
        const mm = String(selectedMonth + 1).padStart(2, '0');
        const dd = String(selectedDay).padStart(2, '0');
        return `${selectedYear}-${mm}-${dd}`;
    }

    async function fetchDayData() {
        loading = true;
        fetchError = '';
        dayData = null;

        try {
            const res = await fetch(
                `http://localhost:5075/tech/calendar?date=${selectedDateString()}`,
                { headers: { Authorization: `Bearer ${auth.token}` } }
            );

            if (!res.ok) {
                fetchError = res.status === 401
                    ? 'Session expired. Please log in again.'
                    : 'Failed to load data.';
                return;
            }

            dayData = await res.json() as CalendarDayResponse;
        } catch {
            fetchError = 'Network error. Is the server running?';
        } finally {
            loading = false;
        }
    }

    function formatDisplayDate() {
        const d = new Date(selectedYear, selectedMonth, selectedDay);
        return d.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
    }

    // ── Init ─────────────────────────────────────────────────────────────────
    onMount(() => { fetchDayData(); });
</script>

<NavigationBar />

<main class="calendar-page">
    <div class="calendar-layout">

        <!-- ── Left: Monthly Calendar ── -->
        <section class="calendar-panel">
            <div class="cal-header">
                <button class="nav-btn" onclick={prevMonth} aria-label="Previous month">&#8249;</button>
                <span class="month-label">{MONTH_NAMES[viewMonth]} {viewYear}</span>
                <button class="nav-btn" onclick={nextMonth} aria-label="Next month">&#8250;</button>
            </div>

            <div class="cal-grid">
                {#each DAY_NAMES as name}
                    <div class="dow-label">{name}</div>
                {/each}

                {#each cells as cell}
                    {#if cell.day === null}
                        <div class="cal-cell empty"></div>
                    {:else}
                        <button
                            class="cal-cell day-btn"
                            class:today={isToday(cell.day)}
                            class:selected={isSelected(cell.day)}
                            onclick={() => selectDay(cell.day!)}
                        >
                            {cell.day}
                        </button>
                    {/if}
                {/each}
            </div>
        </section>

        <!-- ── Right: Day Panel ── -->
        <section class="day-panel">
            <h2 class="day-heading">{formatDisplayDate()}</h2>

            {#if loading}
                <div class="loading-state">Loading...</div>

            {:else if fetchError}
                <div class="error-state">{fetchError}</div>

            {:else if dayData}
            
                <!-- Appointments -->
                <div class="panel-section">
                    <div class="section-header">
                        <h3 class="section-title">Appointments</h3>
                        <button class="new-item-btn" onclick={() => goto(`/manage?type=appointment&id=new&date=${selectedDateString()}`)}>＋ New</button>
                    </div>
                    {#if dayData.appointments.length === 0}
                        <p class="empty-state">No appointments for this date.</p>
                    {:else}
                        {#each dayData.appointments as appt}
                            <div class="appt-card"
                                onclick={() => goto(`/manage?type=appointment&id=${appt.id}`)}
                                role="button" tabindex="0"
                                onkeydown={(e) => e.key === 'Enter' && goto(`/manage?type=appointment&id=${appt.id}`)}>

                                <div class="appt-time">{appt.time}</div>
                                <div class="appt-details">
                                    <span class="appt-client">{appt.clientName}</span>
                                    <span class="appt-service">{appt.serviceName} ({appt.serviceDuration} min)</span>
                                </div>
                                <span
                                    class="badge"
                                    class:badge-confirmed={appt.status === 'confirmed'}
                                    class:badge-pending={appt.status === 'pending'}
                                >{appt.status}</span>
                            </div>
                        {/each}
                    {/if}
                </div>

                <!-- Schedules -->
                <div class="panel-section">
                    <div class="section-header">
                        <h3 class="section-title">Schedules</h3>
                        <button class="new-item-btn" onclick={() => goto(`/manage?type=schedule&id=new&date=${selectedDateString()}`)}>＋ New</button>
                    </div>
                    {#if dayData.schedules.length === 0}
                        <p class="empty-state">No active schedules for this date.</p>
                    {:else}
                        {@const visibleSchedules = dayData.schedules.filter(s => s.outage || s.timeRangesForDay.length > 0)}
                        {#if visibleSchedules.length === 0}
                            <p class="day-off">Day off</p>
                        {:else}
                            {#each visibleSchedules as schedule}
                                <div class="schedule-card" class:outage={schedule.outage}
                                    onclick={() => goto(`/manage?type=schedule&id=${schedule.id}`)}
                                    role="button" tabindex="0"
                                    onkeydown={(e) => e.key === 'Enter' && goto(`/manage?type=schedule&id=${schedule.id}`)}>

                                    <div class="schedule-dates">
                                        <span>From {schedule.effStartDate}</span>
                                        {#if schedule.effEndDate}
                                            <span> — {schedule.effEndDate}</span>
                                        {:else}
                                            <span> — ongoing</span>
                                        {/if}
                                    </div>

                                    {#if schedule.outage}
                                        <span class="badge badge-outage">Outage</span>
                                    {:else}
                                        <ul class="time-ranges">
                                            {#each schedule.timeRangesForDay as range}
                                                <li>{range.beginTime} – {range.endTime}</li>
                                            {/each}
                                        </ul>
                                    {/if}
                                </div>
                            {/each}
                        {/if}
                    {/if}
                </div>

            {/if}
        </section>

    </div>
</main>

<Footer />

<style>
    .calendar-page {
        min-height: calc(100vh - var(--nav-height));
        background: var(--beige-color, #f5f0e8);
        padding: 2rem 1.5rem;
        margin-top: var(--nav-height);
    }

    .calendar-layout {
        display: grid;
        grid-template-columns: 1fr 1.5fr;
        gap: 2rem;
        max-width: 1400px;
        margin: 0 auto;
        align-items: start;
    }

    /* ── Calendar panel ── */
    .calendar-panel {
        background: var(--white-color, #fff);
        border-radius: 12px;
        padding: 1.25rem;
        box-shadow: 0 2px 8px rgba(0,0,0,0.08);
    }

    .cal-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 1rem;
    }

    .month-label {
        font-weight: 600;
        font-size: 1rem;
    }

    .nav-btn {
        background: none;
        border: none;
        font-size: 1.6rem;
        cursor: pointer;
        color: var(--black-color, #111);
        line-height: 1;
        padding: 0 0.25rem;
    }

    .nav-btn:hover { color: var(--pink-color, #e0779a); }

    .cal-grid {
        display: grid;
        grid-template-columns: repeat(7, 1fr);
        gap: 2px;
    }

    .dow-label {
        text-align: center;
        font-size: 0.72rem;
        font-weight: 600;
        color: #888;
        padding: 4px 0;
    }

    .cal-cell {
        aspect-ratio: 1;
        display: flex;
        align-items: center;
        justify-content: center;
        font-size: 0.875rem;
        border-radius: 50%;
    }

    .cal-cell.empty { background: transparent; }

    .day-btn {
        background: none;
        border: none;
        cursor: pointer;
        color: var(--black-color, #111);
        transition: background 0.15s;
    }

    .day-btn:hover { background: var(--light-green-color, #d4edda); }

    .day-btn.today {
        font-weight: 700;
        color: var(--green-color, #2e7d32);
    }

    .day-btn.selected {
        background: var(--pink-color, #e0779a);
        color: #fff;
        font-weight: 600;
    }

    .day-btn.today.selected {
        background: var(--pink-color, #e0779a);
    }

    /* ── Day panel ── */
    .day-panel {
        background: var(--white-color, #fff);
        border-radius: 12px;
        padding: 1.5rem;
        box-shadow: 0 2px 8px rgba(0,0,0,0.08);
    }

    .day-heading {
        font-size: 1.15rem;
        font-weight: 700;
        margin: 0 0 1.25rem 0;
        color: var(--black-color, #111);
    }

    .loading-state, .error-state {
        color: #888;
        font-size: 0.95rem;
    }

    .error-state { color: #c0392b; }

    .panel-section { margin-bottom: 1.75rem; }

    .section-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 0.75rem;
        padding-bottom: 0.4rem;
        border-bottom: 1px solid #eee;
    }

    .section-title {
        font-size: 0.85rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.06em;
        color: #888;
        margin: 0;
    }

    .new-item-btn {
        font-size: 0.8rem;
        color: var(--green-color, #769a95);
        background: none;
        border: 1px solid var(--green-color, #769a95);
        border-radius: 20px;
        padding: 2px 10px;
        cursor: pointer;
        font-family: inherit;
        flex-shrink: 0;
    }

    .new-item-btn:hover { background: var(--light-green-color, #bfd6d3); }

    .empty-state {
        font-size: 0.9rem;
        color: #aaa;
        margin: 0;
    }

    /* Schedule card */
    .schedule-card {
        background: var(--beige-color, #f5f0e8);
        border-radius: 8px;
        padding: 0.75rem 1rem;
        margin-bottom: 0.5rem;
        display: flex;
        align-items: center;
        gap: 0.75rem;
        flex-wrap: wrap;
        cursor: pointer;
        transition: transform 0.15s, box-shadow 0.15s;
    }

    .schedule-card:hover {
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(0,0,0,0.12);
    }

    .schedule-card.outage { border-left: 3px solid #e74c3c; }

    .schedule-dates {
        font-size: 0.82rem;
        color: #666;
        flex: 1;
        min-width: 140px;
    }

    .time-ranges {
        list-style: none;
        margin: 0;
        padding: 0;
        display: flex;
        flex-wrap: wrap;
        gap: 0.4rem;
    }

    .time-ranges li {
        background: var(--green-color, #2e7d32);
        color: #fff;
        font-size: 0.8rem;
        padding: 2px 8px;
        border-radius: 20px;
    }

    .day-off {
        font-size: 0.85rem;
        color: #aaa;
        margin: 0;
    }

    /* Appointment card */
    .appt-card {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        background: var(--beige-color, #f5f0e8);
        border-radius: 8px;
        padding: 0.65rem 1rem;
        margin-bottom: 0.5rem;
        flex-wrap: wrap;
        cursor: pointer;
        transition: transform 0.15s, box-shadow 0.15s;
    }

    .appt-card:hover {
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(0,0,0,0.12);
    }

    .appt-time {
        font-weight: 700;
        font-size: 1rem;
        min-width: 52px;
        color: var(--black-color, #111);
    }

    .appt-details {
        display: flex;
        flex-direction: column;
        flex: 1;
    }

    .appt-client {
        font-weight: 600;
        font-size: 0.95rem;
    }

    .appt-service {
        font-size: 0.82rem;
        color: #666;
    }

    /* Badges */
    .badge {
        font-size: 0.72rem;
        font-weight: 700;
        text-transform: uppercase;
        padding: 2px 8px;
        border-radius: 20px;
        letter-spacing: 0.04em;
    }

    .badge-confirmed { background: var(--green-color, #2e7d32); color: #fff; }
    .badge-pending   { background: var(--warm-pink-color, #e0779a); color: #fff; }
    .badge-outage    { background: #e74c3c; color: #fff; }

    /* ── Responsive ── */
    @media (max-width: 768px) {
        .calendar-layout {
            grid-template-columns: 1fr;
        }
    }
</style>
