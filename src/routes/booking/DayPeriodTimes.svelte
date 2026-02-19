<script lang="ts">
	import { collapseTextChangeRangesAcrossMultipleVersions } from "typescript";
    import type { Slot } from '$lib/types/Slot';;
	import { onMount } from "svelte";
   
    let { dayPeriodSlots, selectedSlot = $bindable(), dayPeriod } = $props();

    function formatTime(timeString: string, locale = navigator.language) {
        const [hours, minutes, seconds] = timeString.split(':').map(Number);

        const date = new Date();

        date.setHours(hours, minutes, seconds ?? 0, 0);

        return new Intl.DateTimeFormat(locale, {
        hour: 'numeric',
        minute: '2-digit'
        }).format(date);
    }

    // on odd-numbered clicks, this will hide all other time slots
    // on even-numbered clicks, this will show them all again
    function selectSlot(slot: Slot) {
        if (selectedSlot.startTime === slot?.startTime)
        {
            selectedSlot.startTime = null;
            return;
        }
        selectedSlot.startTime = slot.startTime;
    }

    function sameSlot(a: Slot | null, b: Slot | null) {
        return (
            a?.startTime === b?.startTime &&
            a?.date === b?.date &&
            a?.techId === b?.techId && 
            a?.service?.id === b?.service?.id
        );
    }
</script>

{#if (dayPeriodSlots?.length > 0 )}
    {#if selectedSlot.startTime == null }
        <h4>{dayPeriod}</h4>
    {/if}
    <div class="slots-carousel">
        {#each dayPeriodSlots as slot}
            {#if (selectedSlot.startTime == null || sameSlot(selectedSlot, slot))}
                <button
                    class="slot selection-button {sameSlot(selectedSlot, slot) ? 'selected' : ''}"
                    type="button"
                    onclick={() => selectSlot(slot)}
                >
                    {formatTime(slot.startTime)}     
                </button>
            {/if}
        {/each}
    </div>
{/if}

<style>
  h4 {
    text-align: center;
  }

  .slots-carousel {
    display: flex;
    width: 100%;  /*needed to make sure selected slot stays in center */
    flex-wrap: wrap;
    flex-direction: row;
    gap: var(--button-gap);
    justify-content: center;
    align-items: center;
    padding-left: 1.5rem;
    padding-right: 1.5rem;
  }

  .slot {
    height: 6vh;
    padding: 3px 3px;
    border-radius: 8px;
    cursor: pointer;
    transition: transform 0.2s, background-color 0.2s;
  }
</style>