// Register plugins once (access globals from window)
window.gsap.registerPlugin(window.ScrollTrigger, window.SplitText);  //ScollSmoother removed

// Import animation modules (they just export init functions)
import { initPanelPinning } from "./animations/panel-pin.js";
import { initServicesBounce } from "./animations/services-bounce.js";
import {initServicesHighlight } from "./animations/services-highlight.js";
import { initButtonAnimation } from "./animations/button-slide.js";
import { initImagesTween } from "./animations/nailart-images.js";

// Single load listener calls all animations
window.addEventListener("load", () => {
    initPanelPinning();
    initServicesBounce();
    initServicesHighlight();
    initButtonAnimation();
    initImagesTween();
});