<script lang="ts">
    import { goto } from "$app/navigation";
    import NavigationBar from "../NavigationBar.svelte";
    import Footer from "../Footer.svelte";
    import { auth } from "$lib/stores/auth";

    let clientEmail = '';
    let clientPassword = '';
    let errorMessage = '';

    async function requestLogin(event: SubmitEvent) {
        event.preventDefault();
        errorMessage = '';

        const payload = {
            Email: clientEmail,
            Password: clientPassword
        };

        const res = await fetch("http://localhost:5075/auth/login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!res.ok) {
            errorMessage = "Invalid email or password.";
            return;
        }

        const data = await res.json();
        auth.login(data.token);
        goto('/calendar');
    }
</script>

<NavigationBar />
<section class="panel warm-pink-bg">
<form onsubmit={requestLogin}>
    <div class="login-form">
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
    <div class="login-form">
        <label for="password">Password</label>

        <input
            bind:value={clientPassword}
            type="password"
            id="password"
            name="password"
            required
            minlength="2"
        />
    </div>
    {#if errorMessage}
        <div class="login-form">
            <p class="error-message">{errorMessage}</p>
        </div>
    {/if}
    <div class="login-form">
        <input
            class="booking-button"
            value="Log In"
            type="submit"
        />
    </div>
    <div class="login-form">
        <a href="/auth/request-password-reset">Forgot Password?</a>
    </div>
</form>
</section>
<Footer />

<style>
    .error-message {
        color: #c0392b;
        margin: 0;
        font-size: 0.9rem;
    }
</style>
