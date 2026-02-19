<script lang="ts">
	  import DayPeriodTimes from "./DayPeriodTimes.svelte";
    import type { Slot } from '$lib/types/Slot';

    const MORNING_START = 5 * 60;
    const AFTERNOON_START = 12 * 60;
    const EVENING_START = 17 * 60;

    let { dateToSlotsDict, selectedSlot = $bindable() } = $props();  //passed from parent

    let selectedPeriod = $derived(() => {
      if (!selectedSlot.startTime) return null;

      const mins = timeStringToMinutes(selectedSlot.startTime);
      if (mins >= MORNING_START && mins < AFTERNOON_START) return "Morning";
      if (mins >= AFTERNOON_START && mins < EVENING_START) return "Afternoon";
      return "Evening";

    });

    // TODO: move these three into a single helper function
    let morningSlots: Slot[] = $derived(dateToSlotsDict[selectedSlot.date].filter((slot : Slot) => {
      const minutes = timeStringToMinutes(slot.startTime);
      if (minutes == -1) return false;
      return minutes >= MORNING_START && minutes < AFTERNOON_START;
    }));

    let afternoonSlots: Slot[] = $derived(dateToSlotsDict[selectedSlot.date].filter((slot : Slot) => {
      const minutes = timeStringToMinutes(slot.startTime);
      if (minutes == -1) return false;
      return minutes >= AFTERNOON_START && minutes < EVENING_START;
    }));

    let eveningSlots: Slot[] = $derived(dateToSlotsDict[selectedSlot.date].filter((slot : Slot) => {
      const minutes = timeStringToMinutes(slot.startTime);
      if (minutes == -1) return false;
      return minutes >= EVENING_START || minutes < MORNING_START;
    }));

    function timeStringToMinutes(time: string | null)
    {
      if (time == null) return -1;
      const [hours, minutes] = time.split(':').map(Number);
      return hours * 60 + minutes;
    }
</script>

<div class="available-times">
  <DayPeriodTimes dayPeriod={"Morning"} dayPeriodSlots = { morningSlots } bind:selectedSlot/>
  <DayPeriodTimes dayPeriod={"Afternoon"} dayPeriodSlots = { afternoonSlots } bind:selectedSlot/>
  <DayPeriodTimes dayPeriod={"Evening"} dayPeriodSlots = { eveningSlots } bind:selectedSlot/>
</div>


<style>
  .available-times {
    width: 100%; /* needed to make sure selected slot stays in center */
    justify-content: center;
    text-align: center;
    align-items: center;
  }
</style>