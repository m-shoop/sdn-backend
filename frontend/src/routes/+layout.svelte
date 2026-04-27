<script lang="ts">
	import favicon from '$lib/assets/favicon.png';
	import '../app.css';
	import { afterNavigate } from '$app/navigation';

	let { children } = $props();

	// Function to initialize all GSAP animations
	async function initAnimations() {
		// Wait for GSAP to be loaded
		if (typeof window === 'undefined' || !window.gsap) {
			return;
		}

		// Clean up existing ScrollTriggers to prevent duplicates
		window.ScrollTrigger?.getAll()?.forEach((trigger: any) => trigger.kill());

		// Register GSAP plugins
		window.gsap.registerPlugin(window.ScrollTrigger, window.SplitText);

		// Dynamically import and run animation modules
		try {
			const { initServicesBounce } = await import('$lib/animations/services-bounce');
			const { initServicesHighlight } = await import('$lib/animations/services-highlight');
			const { initButtonAnimation } = await import('$lib/animations/button-slide');
			const { initImagesTween } = await import('$lib/animations/nailart-images');

			// Initialize each animation if the function exists
			initServicesBounce?.();
			initServicesHighlight?.();
			initButtonAnimation?.();
			initImagesTween?.();
		} catch (error) {
			console.warn('Error loading animations:', error);
		}
	}

	// Initialize after each navigation (including initial page load).
	// Wait for fonts so SplitText measures characters correctly.
	afterNavigate(() => {
		setTimeout(async () => {
			await document.fonts.ready;
			initAnimations();
		}, 50);
	});
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

{@render children()}
