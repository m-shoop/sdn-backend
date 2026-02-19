export function initServicesHighlight() {
    const allLines = document.querySelectorAll('.line');
    const allImages = document.querySelectorAll('.service-img');
    const servicesDetail = document.querySelector('.service-detail') as HTMLElement | null;
    if (!allLines.length || !allImages.length || !servicesDetail) return;

    const serviceDescriptions = [
        "[manicure]: shaping, cuticle care, and a polished finish to keep your natural nails healthy.",
        "[gel polish]: long-lasting color with a glossy finish that stays beautiful for weeks.",
        "[builder in a bottle]: strengthening overlay, perfect for extra durability and growth.",
        "[nail art]: from subtle details to bold statement designs, fully customized to match your style.",
        "[nail extensions]: added length and shape using professional extension techniques for a longer-lasting set."
    ];

    // Give each image a random rotation and a staggered z-index for the default splatter
    allImages.forEach((img, i) => {
        window.gsap.set(img, {
            rotation: window.gsap.utils.random(-15, 15),
            zIndex: i + 1
        });
    });

    const isTouchDevice = window.matchMedia('(hover: none)').matches;
    let activeIndex: number | null = null;

    function activate(index: number) {
        activeIndex = index;

        // Bring active image to top
        allImages.forEach((img, i) => {
            window.gsap.to(img, {
                zIndex: i === index ? 10 : i + 1,
                duration: 0.1
            });
        });

        // Dim other service text buttons
        allLines.forEach((line, i) => {
            window.gsap.to(line, {
                opacity: i === index ? 1 : 0.3,
                duration: 0.3
            });
        });

        // Show description
        servicesDetail.innerHTML = serviceDescriptions[index];
    }

    /*function deactivate() {
        activeIndex = null;

        // Reset z-index to default stagger
        allImages.forEach((img, i) => {
            window.gsap.to(img, {
                zIndex: i + 1,
                duration: 0.1
            });
        });

        // Reset all text buttons
        allLines.forEach(line => {
            window.gsap.to(line, { opacity: 1, duration: 0.3 });
        });

        // Clear description
        servicesDetail.innerHTML = ".";
    }*/

    if (isTouchDevice) {
        // Mobile: click to toggle
        allLines.forEach((line, index) => {
            line.addEventListener('click', () => {
                /*if (activeIndex === index) {
                    deactivate();
                } else {*/
                    activate(index);
                //}
            });
        });
    } else {
        // Desktop: mouseenter / mouseleave
        allLines.forEach((line, index) => {
            line.addEventListener('mouseenter', () => activate(index));
            //line.addEventListener('mouseleave', () => deactivate());
        });
    }
}
