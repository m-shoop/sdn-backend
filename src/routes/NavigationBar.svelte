<script>
    import { goto } from "$app/navigation";
    import { auth } from "$lib/stores/auth";

    let menuOpen = false;

    function toggleMenu() {
        menuOpen = !menuOpen;
    }

    function closeMenu() {
        menuOpen = false;
    }

    function logout() {
        auth.logout();
        closeMenu();
        goto('/');
    }
</script>

<nav class="navbar">
    <div class="logo">
        <a href="/">ShooperDooper</a>
    </div>

    <button class="menu-toggle" on:click={toggleMenu} aria-label="Toggle menu">
        ☰
    </button>

    <ul class:open={menuOpen}>
        <li><a href="/" on:click={closeMenu}>Home</a></li>
        <li><a href="/about" on:click={closeMenu}>About</a></li>
        {#if auth.role != 'Technician'}
            <li><a href="/booking" on:click={closeMenu}>Appointment</a></li>
        {/if}
        {#if auth.role === 'Technician'}
            <li><a href="/calendar" on:click={closeMenu}>Calendar</a></li>
            <li><a href="/settings" on:click={closeMenu}>Settings</a></li>
            <li><button class="nav-logout" on:click={logout}>Logout</button></li>
        {:else}
            <li><a href="/login" on:click={closeMenu}>Login</a></li>
        {/if}
    </ul>
</nav>

<style>
    .navbar {
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 1rem 1.5rem;
        background: var(--white-color);
        color: var(--black-color);
    }

    .logo a {
        font-weight: bold;
        font-size: 1.2rem;
        color: var(--black-color);
        text-decoration: none;
    }

    ul {
        list-style: none;
        display: flex;
        gap: 1.5rem;
        margin: 0;
        padding: 0;
        align-items: center;
    }

    a {
        color: var(--black-color);
        text-decoration: none;
    }

    a:hover {
        text-decoration: underline;
    }

    .nav-logout {
        background: none;
        border: none;
        cursor: pointer;
        color: var(--black-color);
        font-size: inherit;
        font-family: inherit;
        padding: 0;
    }

    .nav-logout:hover {
        text-decoration: underline;
    }

    .menu-toggle {
        display: none;
        background: none;
        border: none;
        color: var(--black-color);
        font-size: 1.5rem;
        cursor: pointer;
    }

    /* Mobile styles */
    @media (max-width: 768px) {
        .menu-toggle {
            display: block;
        }

        ul {
            position: absolute;
            top: 64px;
            right: 0;
            background: var(--white-color);
            flex-direction: column;
            width: 200px;
            padding: 1rem;
            display: none;
            align-items: flex-start;
        }

        ul.open {
            display: flex;
        }
    }
</style>
