<script lang="ts">
    import DateCarousel from "./DateCarousel.svelte";
    import type { Slot } from '$lib/types/Slot';
    import type { Service } from '$lib/types/Service';
    import { onMount } from "svelte";

    // type for record to map date strings each to a Slot array
    type SlotsByDate = Record<string, Slot[]>;

    // interface object for DTO from backend
    interface TechAvailableSlotsOnDateDto {
        techId: number;
        date: string;
        serviceDto: Service;
        availableStartTimes: string[];
    }

    let { selectedSlot = $bindable(), dateToSlotsDict = $bindable() } = $props();
    let date = new Date(); // today's date
    let dates: string[] = [];
    let slotsReady = $state(false); // controls rendering of child component (date carousel)

    // array of DTOs objects
    let data: TechAvailableSlotsOnDateDto[] = [];

    // default to showing dates for next two weeks
    for (let i = 0; i < 14; i++ )
    {
        dates.push(toDateKey(date));
        date.setDate(date.getDate() + 1);
    }

    // helper function for date string formatting
    function toDateKey(date: Date | string): string {
        const d = typeof date === 'string' ? new Date(date) : date;
        return d.toISOString().split('T')[0]; // YYYY-MM-DD
    }

    async function fetchSlots() {
        if (!dates) return; //safety check
        const payload = { 
            Tech: "1",  // this should be optional, and passed in
            Salon: "1",  // we'll need to pass this into the function
            Service: `${selectedSlot?.service?.id}`, // service ID selected in first screen
            DateBegin: dates[0], 
            DateEnd: dates[dates.length-1]
        };
        const res = await fetch("http://localhost:5075/availability", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        // failed to fetch, log an error
        if (!res.ok) {
            console.error("Error fetching slots", res.status);
            return;
        }

        // response from backend server
        const data: TechAvailableSlotsOnDateDto[] = await res.json();

        for (const tech of data) {
            const dateKey = toDateKey(tech.date);

            dateToSlotsDict[dateKey] = tech.availableStartTimes.map((availableTime) => ({
                startTime: availableTime,
                date: tech.date,
                techId: tech.techId,
                service: tech.serviceDto
            }));
        } 
    }

    // fetch slots when component mounts
    onMount(async () => {
        await fetchSlots();
        slotsReady = true;
    });


</script>

<!-- only render child once results are returned from server -->
{#if (slotsReady)}
    <DateCarousel {dates} bind:selectedSlot {dateToSlotsDict} />
{/if}