<script lang="ts">
    let { selectedSlot, clientEmail = $bindable(), clientName = $bindable(), booked = $bindable(), bookingError = $bindable()} = $props();

    async function requestBooking(event: SubmitEvent) {
        event.preventDefault();
        const payload = { 
            AgreementDate: selectedSlot.date,
            StartTime: selectedSlot.startTime,
            Service: `${selectedSlot.service.id}`,  
            Tech: `${selectedSlot.techId}`, 
            Salon: "1", 
            ClientEmail: clientEmail,
            ClientName: clientName
        };

        const res = await fetch(`${import.meta.env.VITE_API_URL}/booking`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        booked = true;

        // failed to request booking, log an error
        if (!res.ok) {
            bookingError = "Error requesting booking " + res.status;
            console.error(bookingError);
            return;
        }
    }
</script>
<form onsubmit={requestBooking}>
    <div class="contact-form">
        <label for="email">Email</label>
        
        <input 
            bind:value={clientEmail} 
            type="email" 
            name="email"
            required
            minlength="3"
            autocomplete="email"
        />
    </div>
    <div class="contact-form">
        <label for="name">Name (Last Name, First Name(s))</label>

        <input
            type="text"
            id="name"
            name="name"
            required
            minlength="2"
            autocomplete="name"
            bind:value={clientName}
        />
    </div>
    <div class="contact-form">
        <input
            class="booking-button"
            value="Book Appointment"
            type="submit"
        />
    </div>
</form>

<style>
    .contact-form {
        justify-content: center;
        text-align: center;
        font-size: 0.85rem;
    }

    .booking-button {
        margin: .25rem;
    }
</style>