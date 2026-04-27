export function initServicesHighlight() {
    const allLines = document.querySelectorAll('.line');
    const allUnits = document.querySelectorAll('.hanging-unit');
    const servicesDetail = document.querySelector('.service-detail') as HTMLElement | null;
    if (!allLines.length || !allUnits.length || !servicesDetail) return;

    const serviceDescriptions = [
        "[manicure]: shaping, cuticle care, and a polished finish to keep your natural nails healthy.",
        "[gel polish]: long-lasting color with a glossy finish that stays beautiful for weeks.",
        "[builder in a bottle]: strengthening overlay, perfect for extra durability and growth.",
        "[nail art]: from subtle details to bold statement designs, fully customized to match your style.",
        "[nail extensions]: added length and shape using professional extension techniques for a longer-lasting set."
    ];

    // Set initial circular clip-path on all buttons based on their height
    allLines.forEach(line => {
        const el = line as HTMLElement;
        const r = el.offsetHeight / 2;
        window.gsap.set(el, { clipPath: `circle(${r}px at 50% 50%)`, opacity: 0.6 });
    });

    // Hide all hanging units on init
    window.gsap.set(allUnits, { autoAlpha: 0 });

    const isTouchDevice = window.matchMedia('(hover: none)').matches;
    let activeIndex: number | null = null;

    function contractButton(index: number) {
        const line = allLines[index] as HTMLElement;
        const r = line.offsetHeight / 2;
        window.gsap.to(line, {
            clipPath: `circle(${r}px at 50% 50%)`,
            opacity: 0.6,
            duration: 0.2,
            ease: 'power2.in'
        });
    }

    function activate(index: number) {
        // Contract the previously active button before activating the new one
        if (activeIndex !== null && activeIndex !== index) {
            contractButton(activeIndex);
        }
        activeIndex = index;

        const unit = allUnits[index] as HTMLElement;
        const line = allLines[index] as HTMLElement;

        // Bring this unit to the front
        allUnits.forEach((u, i) => {
            window.gsap.set(u, { zIndex: i === index ? 10 : i + 1 });
        });

        // Reset transform so fromTo starts clean
        window.gsap.set(unit, { rotation: 0, transformOrigin: '50% 0%' });

        // Flashlight: expand button clip-path from circle to full
        const expandRadius = Math.max(line.offsetWidth, line.offsetHeight) * 1.5;
        window.gsap.to(line, {
            clipPath: `circle(${expandRadius}px at 50% 50%)`,
            opacity: 1,
            duration: 0.2,
            ease: 'power2.out'
        });

        // Image splashes in: swings from the side like a pendant (pivot at image top)
        window.gsap.fromTo(
            unit,
            { autoAlpha: 0, rotation: 18, transformOrigin: '50% 0%' },
            { autoAlpha: 1, rotation: 0, duration: 1.1, delay: 0.05, ease: 'elastic.out(1, 0.45)' }
        );

        if (servicesDetail) servicesDetail.innerHTML = serviceDescriptions[index];
    }

    // Show first service on init
    activate(0);

    if (isTouchDevice) {
        allLines.forEach((line, index) => {
            line.addEventListener('click', (e) => {
                e.stopPropagation();
                activate(index);
            });
            // Stop touchstart propagation so the document handler below doesn't fire when touching a button
            line.addEventListener('touchstart', (e) => {
                e.stopPropagation();
            }, { passive: true });
        });

        // Contract active button when touching anywhere outside the buttons.
        // Use touchstart — more reliable than click on non-interactive areas in mobile Safari.
        document.addEventListener('touchstart', () => {
            if (activeIndex !== null) {
                contractButton(activeIndex);
                activeIndex = null;
            }
        }, { passive: true });
    } else {
        allLines.forEach((line, index) => {
            line.addEventListener('mouseenter', () => activate(index));
            line.addEventListener('mouseleave', () => contractButton(index));
        });
    }
}
