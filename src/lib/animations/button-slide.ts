export function initButtonAnimation() {
    const buttons = document.querySelectorAll(".schedule-button")
    if (!buttons.length) return;

    const schedtl = window.gsap.timeline({
        scrollTrigger: {
            trigger: '.sched-panel',
            start: "top 35%",
            toggleActions: "restart none none reverse",
            // markers: true,
        }
    });


    // Tween 1
    schedtl.fromTo(".sched-anim1",
        {x: -1000, rotation: -180},
        {x: '0vw', duration: 0.8, ease: "back.out", rotation: 0},
        0
    );


    // Catch any window resizings with debounce to avoid race conditions with PanelPinning.js
    //let resizeTimeout;
    //window.addEventListener("resize", () => {
    //    clearTimeout(resizeTimeout);
    //    resizeTimeout = setTimeout(() => {
    //        window.ScrollTrigger.refresh();
    //    }, 250);
    //});
}