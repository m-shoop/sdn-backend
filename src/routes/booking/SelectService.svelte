<script lang="ts">
    import type { Service } from '$lib/types/Service';
    import type { Slot } from '$lib/types/Slot';
    import { sameSlot } from '$lib/utils/slot'; 
    import { onMount } from "svelte";

    let { selectedSlot = $bindable<Slot | null>() } = $props();
    let loadingServices = $state(true);
    let servicesData : Salon | null = $state(null);

    // when supporting more than one salon, we'll have to expand this out more
    // see issue SHO-74
    interface Salon {
        salonId: number,
        techServDtoList: TechServices[];
    }
    
    interface TechServices {
        techId: number;
        techName: string;
        servicesDtoList: Service[];
    }

    async function fetchServices() {
        loadingServices = true;

        const payload = { 
            Tech: "1",  // hard-coded - see issue SHO-74
            Salon: "1",  // hard-coded - see issue SHO-74
        };

        const res = await fetch(`${import.meta.env.VITE_API_URL}/services`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            servicesData = await res.json();
        }

        else {
            console.error("Error fetching slots", res.status);
        }

        loadingServices = false;
    }

    // fetch services when component mounts
    onMount(async () => {
        await fetchServices();
    });

    function updateService(service: Service) {
        if (service === null) {
            return
        }
        else if (service.id === selectedSlot.service.id) {
            updateSelectedSlotService(null, null, null);
        }
        else {
            updateSelectedSlotService(service.id, service.name, service.duration);
        };    
    }

    function updateSelectedSlotService(id: number | null, name: string | null, duration: number | null) {
        selectedSlot.service.id = id;
        selectedSlot.service.name = name;
        selectedSlot.service.duration = duration;
        // hardcode to a single nail tech for now
        selectedSlot.techId = 1;
        // if we're changing the selected service, we need to reset date and startTime
        // as those were based on the previous service selected
        selectedSlot.date = null;
        selectedSlot.startTime = null;
    }
</script>


{#if loadingServices}
    <p>Loading..</p>
{:else if (servicesData?.techServDtoList?.length ?? 0) > 0}
    <div class="service-selection">
        {#each servicesData?.techServDtoList?.[0]?.servicesDtoList ?? [] as service}
            {#if (selectedSlot.service.id === null) || (selectedSlot.service.id === service.id)}
                <button
                    class="service selection-button {selectedSlot.service?.id === service.id ? 'selected' : ''}"
                    type="button" 
                    onclick = {() => updateService(service)} 
                >
                    {service.name} 
                </button>
            {/if}
        {/each}
    </div>
{:else}
<h4>No services are currently available.</h4>
{/if}

<style>
    .service-selection {
        display: flex;
        height: 6vh;
        justify-content: center;
        gap: var(--button-gap);
    }

    .service {
        border-radius: 8px;
    }
</style>

