export function initPanelPinning() {
    const panels = window.gsap.utils.toArray(".pin-panel");
    const nav = document.querySelector('nav');
    if (!panels.length || !nav) return;

    // store window width
    let cachedWindowWidth = 0;

    function updateLayout() {
        const navHeight = nav?.offsetHeight;

        // do nothing if window width stayed the same
        if (window.innerWidth == cachedWindowWidth) return;
        cachedWindowWidth = window.innerWidth;

        // Set CSS variable for panel height
        document.documentElement.style.setProperty("--nav-height", navHeight + "px");

        // Add top padding to DOM so content is not hidden behind nav
        //document.body.style.paddingTop = navHeight + "px";

        window.ScrollTrigger.refresh();
    };

    // initial setup
    updateLayout();

    // if window size changes, update layout
    window.addEventListener("resize", updateLayout);

    // create media query object
    let mm = window.gsap.matchMedia();

    // if the viewport is at least 800px (more than most tablets and mobile devices)
    // add the pinning animation to the website
    mm.add("(min-width: 800px)", () => {
        panels.forEach((panel: HTMLElement, i: number) => {

            window.ScrollTrigger.create({
                trigger: panel,
                start: () => `top top+=${nav.offsetHeight}`,
                end: () => i === panels.length -1
                    ? `${nav.offsetHeight}`
                    : `+=${panel.offsetHeight}`,
                pin: true,
                pinSpacing: i === panels.length - 1 ? true : false,
                scrub: true
            });
        });
    });

}