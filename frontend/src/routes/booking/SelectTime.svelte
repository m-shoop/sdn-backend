<script lang="ts">
    import TimeSlotsGrouper from "./TimeSlotsGrouper.svelte";

    let { selectedSlot = $bindable(), dateToSlotsDict } = $props();

</script>

<!-- date carousel to choose the service date -->
{#if (dateToSlotsDict[selectedSlot.date] != null)}
    <TimeSlotsGrouper {dateToSlotsDict} bind:selectedSlot />
{:else}
<h4>Please select a date.</h4>
{/if}

<style>
    h4 {
        text-align: center;
    }
</style>