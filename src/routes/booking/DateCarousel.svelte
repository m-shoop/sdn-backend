<script lang="ts">
    import type { AvailableSlotsOnDate } from '$lib/types/AvailableSlots';
    import type { Slot } from '$lib/types/Slot';
    import { onMount } from 'svelte';

    interface Props {
        dates: string[];
        selectedDate: string;
    }

    const dateFormatter = new Intl.DateTimeFormat(undefined, {
        weekday: 'short',
        day: 'numeric',
        month: 'short'
    });

    function formatDateKey(dateKey: string): string {
        const [y, m, d] = dateKey.split('-').map(Number);
        return dateFormatter.format(new Date(y, m - 1, d));
    }

    let { dates, selectedSlot=$bindable(), dateToSlotsDict } = $props();
    let carouselElement: HTMLDivElement;

    // on odd-numbered clicks this will collapse the other dates
    // on even-numbered clicks this will make them all appear again
    function selectDate(date: string) {

        if (selectedSlot.date === date)
        {
           selectedSlot.date = null;
           // if we're changing the date, we should reset our time, too
           selectedSlot.startTime = null;
           return;
        }
        // otherwise, we should select this new date
        selectedSlot.date = date;
    }

    // Auto-scroll to first available date when component mounts or data changes
    $effect(() => {
        if (carouselElement && dates.length > 0 && dateToSlotsDict) {
            // Small delay to ensure DOM is fully rendered
            setTimeout(() => {
                const firstAvailableButton = carouselElement.querySelector('.selection-button');
                if (firstAvailableButton) {
                    firstAvailableButton.scrollIntoView({
                        behavior: 'smooth',
                        block: 'nearest',
                        inline: 'center'
                    });
                }
            }, 100);
        }
    });
</script>

<div class="carousel" bind:this={carouselElement}>
    {#each dates as date}
        {#if ((selectedSlot.date === null || selectedSlot.date === date) && (dateToSlotsDict[date]?.length > 0))}
            <button
                class="date-card selection-button
                    {date === selectedSlot.date ? 'selected' : ''}"
                type="button"
                onclick = {() => selectDate(date)}
            >
                {formatDateKey(date)}
            </button>
        {:else if !(dateToSlotsDict[date]?.length > 0) && selectedSlot.date === null}
            <button
                class="date-card grayed-out"
                type="button"
            >
            {formatDateKey(date)}
            </button>
        {/if}
    {/each}
</div>

<style>
    .carousel {
        display: flex;
        gap: var(--button-gap);
        overflow-x: auto;
        scroll-snap-type: x mandatory;
        align-items: center;
    }

    .date-card{
        flex-shrink: 0;
        height: 6vh;
        border-radius: 8px;
        scroll-snap-align: center;
        box-shadow: 0 2px 8px rgba(0,0,0,0.08);
    }

    .grayed-out {
        background: #D3D3D3;
    }
</style>