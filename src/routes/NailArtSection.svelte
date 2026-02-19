<script lang="ts">
</script>

<!-- Nailart Section -->
<section class="panel pin-panel pink-bg nailart-panel">

        <div class="nailart-img img-row">
            <img src="images/RubyRed.JPEG" alt="Demon Slayer anime-inspired nail art"
                class="small-img-rounded nailart1">
        </div>

        <div class="nailart-img img-row">
            <img src="images/Snakeskin.JPEG" alt="snakeskin nail art with blooming gel"
                class="small-img-rounded nailart2">
        </div>

        <div class="nailart-img img-row">
            <img src="images/IcySpicy.JPEG" alt="Wicked-inspired nail art"
                class="small-img-rounded nailart3">
        </div>
</section>

<style>
    .img-row {
        display: flex;
        flex-direction: row;
        margin-bottom: 0.75rem;
    }

    .small-img-rounded {
        border-radius: 20%;
    }

    /* desktop and some tablets */
    @media (min-aspect-ratio: 6/5) {
        .small-img-rounded {
            max-width: 20vw;
        }

    }

    /* up to 6 width : 5 height ratio (most mobile) */
    @media (max-aspect-ratio: 6/5) {
        .small-img-rounded {
            max-width: 48vw;
        }
    }

    @media (max-width: 576px) {
        .img-row {
            padding-bottom: 0.25rem;
        }
    }

</style>