<script lang="ts">

</script>

<!-- Welcome Section -->
<section class="panel pin-panel beige-bg" style="flex-direction: column;">

    <div>
        <h1>Welcome to Shooper Dooper Nails!</h1>
    </div>

    <div>
        <img src="/images/Full-color-Logo.png" alt="Logo" class="img-rounded">
    </div>

</section>

<style>
    /* img-rounded class for rounded corner images */
    .img-rounded {
        border-radius: 20%;
        max-width: 85vw;
    }

    /* desktop and some tablets */
    @media (min-aspect-ratio: 6/5) {
        .img-rounded {
            max-width: 35vw;
        }
    }

    /* up to 6 width : 5 height ratio (most mobile) */
    @media (max-aspect-ratio: 6/5) {
        .img-rounded {
            max-width: 90vw;
        }
    }
</style>