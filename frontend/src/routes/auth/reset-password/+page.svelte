<script lang="ts">
    import { page } from '$app/stores';
    import NavigationBar from "../../NavigationBar.svelte";
    import Footer from "../../Footer.svelte";

    let newPassword = $state('');
    let confirmPassword = $state('');
    let success = $state(false);
    let errorMessage = $state('');

    // Get the token from the URL query parameter
    let token = $derived(new URL($page.url).searchParams.get('token') ?? '');

    async function setPassword(event: SubmitEvent) {
        event.preventDefault();
        errorMessage = '';

        if (newPassword !== confirmPassword) {
            errorMessage = "Passwords do not match.";
            return;
        }

        if (newPassword.length < 8) {
            errorMessage = "Password must be at least 8 characters.";
            return;
        }

        const res = await fetch(`${import.meta.env.VITE_API_URL}/auth/set-password`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ Token: token, NewPassword: newPassword })
        });

        if (res.ok) {
            success = true;
        } else {
            const data = await res.json().catch(() => null);
            errorMessage = data?.message ?? "Invalid or expired reset link. Please request a new one.";
        }
    }
</script>

<NavigationBar />
<section class="panel pink-bg">
    {#if !token}
        <div class="login-form">
            <h2>Invalid Link</h2>
            <p>This password reset link is invalid. Please <a href="/auth/request-password-reset">request a new one</a>.</p>
        </div>
    {:else if success}
        <div class="login-form">
            <h2>Password Set</h2>
            <p>Your password has been set successfully. You can now <a href="/login">log in</a>.</p>
        </div>
    {:else}
        <form onsubmit={setPassword}>
            <div class="login-form">
                <h2>Set New Password</h2>
            </div>
            <div class="login-form">
                <label for="new-password">New Password</label>
                <input
                    bind:value={newPassword}
                    type="password"
                    id="new-password"
                    name="new-password"
                    required
                    minlength="8"
                    autocomplete="new-password"
                />
            </div>
            <div class="login-form">
                <label for="confirm-password">Confirm Password</label>
                <input
                    bind:value={confirmPassword}
                    type="password"
                    id="confirm-password"
                    name="confirm-password"
                    required
                    minlength="8"
                    autocomplete="new-password"
                />
            </div>
            {#if errorMessage}
                <div class="login-form">
                    <p class="error">{errorMessage}</p>
                </div>
            {/if}
            <div class="login-form">
                <input
                    class="booking-button"
                    value="Set Password"
                    type="submit"
                />
            </div>
        </form>
    {/if}
</section>
<Footer />

<style>
    .error {
        color: #a00;
    }
</style>
