<script lang="ts">
    import NavigationBar from '../NavigationBar.svelte'
    import Footer from '../Footer.svelte';
    import SelectService from './SelectService.svelte';
    import SelectDate from './SelectDate.svelte';
    import SelectTime from './SelectTime.svelte';
    import EnterContactDetails from './EnterContactDetails.svelte';
    import type { Slot } from '$lib/types/Slot';

    // dictionary type for available times
    type SlotsByDate = Record<string, Slot[]>;

    // 🔑 build dictionary
    const dateToSlotsDict: SlotsByDate = {};

    let selectedSlot: Slot  = $state({date: null, startTime: null, techId: null, service: {id: null, name: null, duration: null}});
    let clientEmail = $state('');
    let clientName = $state('');  
    let booked = $state(false);
    let bookingError = $state('');
</script>

<NavigationBar />

<section class="panel beige-bg" style="justify-content: start; flex-direction: column;">
<h2>Shooper Dooper Scheduling</h2>
{#if (booked === false)}
    <p class="help-text">To book your next set, select a service, date and time.</p>
    <div class="title-bar beige-bg">
        Service
    </div>
    <div class="horizontal-bar">
        <SelectService bind:selectedSlot />
    </div>
    {#if (selectedSlot?.service?.id != null)}
        <div class="title-bar">
            Date
        </div>
        <div class="horizontal-bar">
            <SelectDate bind:selectedSlot { dateToSlotsDict } />
        </div>
    {/if}
    {#if ( selectedSlot?.date != null )}
        <div class="title-bar">
            Time
        </div>
        <div class="horizontal-bar">
            <SelectTime bind:selectedSlot { dateToSlotsDict }/>
        </div>
    {/if}
    {#if ( selectedSlot?.startTime != null) && booked === false}
    <p class="help-text">Enter your name and email to receive a booking confirmation for your appointment.</p>
    <p class="help-text">You must open the confirmation link from the email to finalize your appointment.</p>
        <div class="title-bar">
            Contact Details
        </div>
        <div class="horizontal-bar" style="flex-direction: column;">
            <EnterContactDetails { selectedSlot } bind:clientEmail bind:clientName bind:booked bind:bookingError/>
        </div>
    {/if}
{/if}
{#if booked === true}
    <div class="final-text">
    {#if (bookingError != '')}
        <div class="help-text">
            Sorry! We seem to have encountered an error. {bookingError}
        </div>
    {:else}
        <div class="help-text">
            <div>Congrats, {clientName}! </div> 
            <div>You've booked an appointment for a {selectedSlot?.service?.name ?? "service"} on {selectedSlot.date} at {selectedSlot.startTime}. </div>
            <div>You will receive an appointment confirmation email (at {clientEmail}) - <b>you must open the confirmation link from that email within 30 minutes or your appointment will be canceled.</b></div>
        </div>
    {/if}
    </div>
{/if}
</section>
<Footer />



<style>
    h2 {
        text-align: center;
    }

    .help-text {
        text-align: center;
        font-size: 0.85rem;
    }

    .help-text > div {
        padding: 1rem;
    }

    .horizontal-bar {
        display: flex;
        width: 100%;
        min-height: 7vh;
        justify-content: center;
        align-items: center;
        font-size: 1.2rem;
    }

    .title-bar {
        display: flex;
        width: 100%;
        min-height: 3vh;
        justify-content: center;
        font-weight: bold;
    }

    .final-text {
        display: flex;
        margin: 1rem;
        border: 2px;
        border-style: solid;
    }
</style>
