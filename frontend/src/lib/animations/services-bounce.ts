export function initServicesBounce() {
    const text = document.querySelector(".services-text");
    const panel = document.querySelector(".services-panel");
    if (!text || !panel) return;

    let split: any;
    let stAnim: any;

    function createSplitAnimation() {
        // Kill old instances
        if (split) split.revert(); // resets DOM to original text
        if (stAnim) stAnim.scrollTrigger.kill();

        // 1. Create SplitText inxtance
        split = window.SplitText.create(".services-text", { type: "words, chars" });

        // 2. Animate words
        stAnim = window.gsap.from(split.chars, {
            scrollTrigger: {
                trigger: '.services-panel',
                start: "top 25%",
                toggleActions: "restart none none reverse",
                // markers: true,
            },
            yPercent: "random([-125, 125])",
            rotation: "random(-30,30)",
            autoAlpha: 0,
            stagger: {
                amount: 0.75,
                from: "random"
            }
        });
    }

    // 3. Initial creation
    createSplitAnimation();

    // 4. Catch any window resizings with debounce to avoid race conditions with PanelPinning.js
    //let resizeTimeout;
    //window.addEventListener("resize", () => {
    //    clearTimeout(resizeTimeout);
    //    resizeTimeout = setTimeout(() => {
    //        window.ScrollTrigger.refresh();
    //    }, 250);
    //});
}
