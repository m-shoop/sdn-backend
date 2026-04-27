<!-- Services Section -->
<script lang="ts">

    const services = [
        {
            name: 'Manicure',
            img: '/images/manicure-service.JPEG',
            alt: 'manicure',
            description: 'Shaping, cuticle care, and a polished finish to keep your natural nails healthy and beautiful.'
        },
        {
            name: 'Gel Polish',
            img: '/images/gelpolish-service.JPEG',
            alt: 'gel polish',
            description: 'Long-lasting colour with a glossy finish that stays beautiful for weeks without chipping.'
        },
        {
            name: 'Builder In A Bottle',
            img: '/images/biab-service.JPEG',
            alt: 'builder in a bottle',
            description: 'A strengthening overlay perfect for extra durability and growth support on natural nails.'
        },
        {
            name: 'Nail Art',
            img: '/images/nailart-service.JPEG',
            alt: 'nail art',
            description: 'From subtle details to bold statement designs, fully customised to match your style and personality.'
        },
        {
            name: 'Nail Extensions',
            img: '/images/extensions-service.JPEG',
            alt: 'nail extensions',
            description: 'Added length and shape using professional extension techniques for a longer-lasting, stunning set.'
        }
    ];

    let current = $state(0);

    let nameEl: HTMLElement | undefined;
    // Track the active SplitText instance and its chars so we can interrupt cleanly
    let activeSplit: any = null;
    let activeChars: Element[] = [];

    function animateName(newName: string, dir: 1 | -1) {
        if (typeof window === 'undefined' || !window.gsap || !window.SplitText || !nameEl) {
            if (nameEl) nameEl.textContent = newName;
            return;
        }

        // Kill any in-progress tween and restore the DOM before starting fresh
        if (activeChars.length) window.gsap.killTweensOf(activeChars);
        if (activeSplit) { activeSplit.revert(); activeSplit = null; activeChars = []; }

        // ── Exit: snake letters out ──
        activeSplit = window.SplitText.create(nameEl, { type: 'words,chars' });
        const outSplit = activeSplit;
        activeChars = [...outSplit.chars];

        window.gsap.to(activeChars, {
            x: dir * -45,
            y: -28,
            rotation: dir * 90,
            scale: 0.2,
            opacity: 0,
            stagger: { amount: 0.22, from: dir === 1 ? 'start' : 'end' },
            duration: 0.26,
            ease: 'power2.in',
            onComplete: () => {
                // Guard: a newer call may have already taken over
                if (activeSplit !== outSplit) return;

                outSplit.revert();
                activeSplit = null;
                activeChars = [];

                if (!nameEl) return;
                nameEl.textContent = newName;

                // ── Entry: snake new letters in from the opposite side ──
                activeSplit = window.SplitText.create(nameEl, { type: 'words,chars' });
                const inSplit = activeSplit;
                activeChars = [...inSplit.chars];

                window.gsap.from(activeChars, {
                    x: dir * 45,
                    y: -28,
                    rotation: dir * -90,
                    scale: 0.2,
                    opacity: 0,
                    stagger: { amount: 0.32, from: dir === 1 ? 'start' : 'end' },
                    duration: 0.40,
                    ease: 'back.out(2)',
                    onComplete: () => {
                        if (activeSplit === inSplit) {
                            inSplit.revert();
                            activeSplit = null;
                            activeChars = [];
                        }
                    }
                });
            }
        });
    }

    function goTo(index: number, dir: 1 | -1) {
        current = index;
        animateName(services[index].name, dir);
    }

    function prev() {
        goTo((current - 1 + services.length) % services.length, -1);
    }

    function next() {
        goTo((current + 1) % services.length, 1);
    }

    // Swipe support
    let touchStartX = 0;
    function onTouchStart(e: TouchEvent) {
        touchStartX = e.touches[0].clientX;
    }
    function onTouchEnd(e: TouchEvent) {
        const diff = touchStartX - e.changedTouches[0].clientX;
        if (diff > 50) goTo((current + 1) % services.length, 1);
        else if (diff < -50) goTo((current - 1 + services.length) % services.length, -1);
    }
</script>

<section class="panel green-bg services-panel">

    <!-- Left: section title + intro -->
    <div class="services-div-first">
        <span class="services-text">
            <h2 id="services">Services</h2>
            <p class="text-box">
                At Shooper Dooper Nails, I offer a range of nail services, from natural nail care to creative, custom
                sets.
                <br /><br />
                Whether you prefer something simple and elegant or bold and expressive, I'll help bring your vision to
                life.
            </p>
        </span>
    </div>

    <!-- Right: badge + carousel side by side -->
    <div class="services-right">

        <!-- Animated name badge -->
        <div class="name-badge">
            <span class="name-text" bind:this={nameEl}>{services[0].name}</span>
        </div>

        <!-- Carousel -->
        <div class="carousel-stack">
            <div class="carousel-nav">
                <!-- Left arrow -->
                <button class="arrow-btn" onclick={prev} aria-label="Previous service">&#8249;</button>

                <!-- Card track -->
                <div class="carousel-viewport"
                     role="region"
                     aria-label="Services carousel"
                     ontouchstart={onTouchStart}
                     ontouchend={onTouchEnd}>
                    <div
                        class="carousel-track"
                        style="transform: translateX(-{current * (100 / services.length)}%)"
                    >
                        {#each services as service}
                            <div class="service-card">
                                <img src={service.img} alt={service.alt} class="card-img" />
                                <div class="card-body">
                                    <p class="card-label">{service.name}</p>
                                    <p class="card-desc">{service.description}</p>
                                </div>
                            </div>
                        {/each}
                    </div>
                </div>

                <!-- Right arrow -->
                <button class="arrow-btn" onclick={next} aria-label="Next service">&#8250;</button>
            </div>

            <!-- Dot indicators -->
            <div class="dots" role="tablist" aria-label="Service indicators">
                {#each services as _, i}
                    <button
                        class="dot"
                        class:active={i === current}
                        onclick={() => goTo(i, i >= current ? 1 : -1)}
                        aria-label="Go to {services[i].name}"
                        aria-selected={i === current}
                        role="tab"
                    ></button>
                {/each}
            </div>
        </div>

    </div>
</section>

<style>
    /* ── Layout ── */

    :global(.services-panel) {
        gap: 2rem;
    }

    .services-div-first {
        width: 40%;
        padding: 0 3rem 0 6rem;
        display: flex;
        flex-direction: column;
        align-items: center;
    }

    .services-right {
        width: 60%;
        display: flex;
        flex-direction: row;
        align-items: center;
        justify-content: center;
        gap: 1.5rem;
    }

    .carousel-stack {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 1rem;
    }

    /* ── Circular name badge ── */
    .name-badge {
        width: 205px;
        height: 205px;
        border-radius: 50%;
        background: var(--beige-color);
        box-shadow: 0 4px 18px rgba(0, 0, 0, 0.14);
        display: flex;
        align-items: center;
        justify-content: center;
        overflow: visible; /* letters animate in/out without being clipped by badge edge */
    }

    .name-text {
        font-size: 1.05rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.08em;
        color: var(--green-color);
        text-align: center;
        line-height: 1.4;
        padding: 0 1.4rem;
        /* SplitText will split this into individual chars for animation */
    }

    /* ── Carousel structure ── */
    .carousel-nav {
        display: flex;
        align-items: center;
        gap: 0.85rem;
        width: 100%;
        justify-content: center;
    }

    .carousel-viewport {
        flex: 0 1 350px;
        overflow: hidden;
        border-radius: 12px;
    }

    .carousel-track {
        display: flex;
        width: calc(100% * 5); /* 5 cards wide */
        transition: transform 0.38s cubic-bezier(0.25, 0.46, 0.45, 0.94);
    }

    /* ── Card ── */
    .service-card {
        flex: 0 0 20%; /* 1/5 of track = 1 viewport width */
        background: var(--white-color);
        border-radius: 12px;
        overflow: hidden;
        box-shadow: 0 4px 20px rgba(0, 0, 0, 0.13);
    }

    .card-img {
        width: 100%;
        aspect-ratio: 4 / 5;
        object-fit: cover;
        display: block;
    }

    .card-body {
        padding: 0.85rem 1rem 1rem;
    }

    .card-label {
        font-size: 0.78rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--green-color);
        margin: 0 0 0.4rem;
        text-align: left;
    }

    .card-desc {
        font-size: 0.82rem;
        color: #555;
        line-height: 1.55;
        margin: 0;
        text-align: left;
    }

    /* ── Arrow buttons ── */
    .arrow-btn {
        flex-shrink: 0;
        width: 2.4rem;
        height: 2.4rem;
        border-radius: 50%;
        border: 2px solid rgba(255, 255, 255, 0.45);
        background: rgba(255, 255, 255, 0.15);
        color: var(--white-color);
        font-size: 1.6rem;
        line-height: 1;
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center;
        transition: background 0.15s, border-color 0.15s, transform 0.1s;
        font-family: inherit;
    }

    .arrow-btn:hover {
        background: rgba(255, 255, 255, 0.3);
        border-color: rgba(255, 255, 255, 0.75);
    }

    .arrow-btn:active {
        transform: scale(0.93);
    }

    /* ── Dots ── */
    .dots {
        display: flex;
        gap: 0.45rem;
        align-items: center;
    }

    .dot {
        width: 7px;
        height: 7px;
        border-radius: 50%;
        border: none;
        padding: 0;
        background: rgba(255, 255, 255, 0.35);
        cursor: pointer;
        transition: background 0.2s, transform 0.2s;
    }

    .dot.active {
        background: var(--pink-color);
        transform: scale(1.4);
    }

    /* ── Mobile ── */
    @media (max-aspect-ratio: 6/5) {
        :global(.services-panel) {
            gap: 0.5rem;
            overflow: visible; /* panel pinning is off on mobile, overflow:hidden just clips content */
        }

        .services-div-first {
            width: 100%;
            padding: 2rem 1.25rem 0;
        }

        .services-right {
            width: 100%;
            flex-direction: column;
            gap: 0;
        }

        .name-badge {
            width: 170px;
            height: 170px;
            margin: 0.9rem 0;
        }

        .carousel-stack {
            padding-bottom: 0.75rem;
            max-width: 78vw;
        }

        .carousel-viewport {
            flex: 0 1 72vw;
        }
    }
</style>
