<script lang="ts">
    import NavigationBar from "../../NavigationBar.svelte";
    import Footer from "../../Footer.svelte";
    let email = '';
    let submitted = false;
    let errorMessage = '';

    async function requestReset(event: SubmitEvent) {
        event.preventDefault();
        errorMessage = '';

        const res = await fetch("http://localhost:5075/auth/request-password-reset", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ Email: email })
        });

        if (res.ok) {
            submitted = true;
        } else {
            errorMessage = "Something went wrong. Please try again.";
        }
    }
</script>

<NavigationBar />
<section class="panel pink-bg">
    {#if submitted}
        <div class="login-form">
            <h2>Check your email</h2>
            <p>If an account exists for {email}, we've sent a password reset link. The link expires in 30 minutes.</p>
        </div>
    {:else}
        <form onsubmit={requestReset}>
            <div class="login-form">
                <h2>Reset Password</h2>
                <p>Enter your email and we'll send you a link to set a new password.</p>
            </div>
            <div class="login-form">
                <label for="email">Email</label>
                <input
                    bind:value={email}
                    type="email"
                    name="email"
                    required
                    minlength="3"
                    autocomplete="email"
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
                    value="Send Reset Link"
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
