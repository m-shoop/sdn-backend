export function initImagesTween () {
    const images = document.querySelectorAll(".nailart1");
    if (!images.length) return;

    const tl = window.gsap.timeline({
        scrollTrigger: {
            trigger: '.nailart-panel',
            start: "top 50%",
            toggleActions: "restart none none reverse",
            // markers: true,
        }
    });

    tl.fromTo(".nailart1", 
        {x: 0, scale: 0.0}, 
        {x: '0vw', y: 20, rotation: "random(-20,0)", scale: 1, duration: 0.7, ease: "back.out"},
        0
    );

    // Tween 2
    tl.fromTo(".nailart2", 
        {x: 0, scale: 0.0}, 
        {x: '-5vw', y: 0, rotation: "random(0,20)", scale: 1, duration: 0.7, ease: "back.out"},
        "<0.2"
    );

    // Tween 3
    tl.fromTo(".nailart3", 
        {x: 0, scale: 0.0}, 
        {x: '-5vw', y: 0, rotation: "random(-20,0)", scale: 1, duration: 0.7, ease: "back.out"},
        "<0.2"
    );
}