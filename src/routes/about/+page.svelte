<script lang="ts">
    import NavigationBar from '../NavigationBar.svelte';
    import Footer from '../Footer.svelte';
</script>

<NavigationBar />
<section class="panel green-bg">
    <div>
        <img 
        src="/images/TylerBioPic.PNG" 
        alt="Logo" 
        class="img-circular">
    </div>
    <div class="text-box">
        <h1>About me</h1>
        <p>
            Hey! I'm Tyler, the artist behind Shooper Dooper Nails. I'm all about creative nails, geeky good-times, and real connection. I love bringing anime, gaming, and playful vibes into my work but, even more, I love making people feel beautiful when they leave my chair.
            <br /><br />
            My nail obsession started with good friends. We got together monthly to paint our nails and we called ourselves the Den Posh Lacqueristas. 🙂 Fast forward to now: I have honed my craft and am proud of the space I've built where creativity meets technique.
            <br /><br />
            Let's make your nails as fun and unique as you are! Book with me!
            <br />
            <a href="https://www.instagram.com/shooperdoopernails/" class="link-light" target="_blank">@shooperdoopernails</a> 
        </p>
    </div>
</section>
<Footer />

<style>
    .img-circular {
        border-radius: 50%;
        max-width: 42vw;
        padding: 0.5rem;
    }

    /* up to 6 width : 5 height ratio (most mobile) */
    @media (max-aspect-ratio: 6/5) {

        .img-circular {
            max-width: 65vw;
        }
    }

    @media (max-height: 667px) {
        .img-circular {
            max-width: 45vw;
        }
    }
    /*
    .text-box {
        padding: 1.25rem;
    }*/

</style>