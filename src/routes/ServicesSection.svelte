<!-- Services Section -->
<section class="panel pin-panel green-bg services-panel">

    <div class="services-div-first">
        <span class="services-text">
            <h2 id="services">Services</h2>
            <p class="text-box">

                At Shooper Dooper Nails, I offer a range of nail services, from natural nail care to creative, custom
                sets.
                <br /><br />
                Whether you prefer something simple and elegant or bold and expressive, I'll help bring your vision to
                life.

            </p>
        </span>
    </div>
    <div class="services-div-middle services-text">
        <span class="line custom-button beige-bg"><b>manicure</b></span>
        <span class="line custom-button beige-bg"><b>gel polish</b></span>
        <span class="line custom-button beige-bg"><b>builder in a bottle</b></span>
        <span class="line custom-button beige-bg"><b>nail art</b></span>
        <span class="line custom-button beige-bg"><b>nail extensions*</b></span>
    </div>
    <div class="services-div-last">
        <div class="image-stack">
            <img src="/images/manicure-service.JPEG" class="service-img" data-index="0" alt="manicure">
            <img src="/images/gelpolish-service.JPEG" class="service-img" data-index="1" alt="gel polish">
            <img src="/images/biab-service.JPEG" class="service-img" data-index="2" alt="builder in a bottle">
            <img src="/images/nailart-service.JPEG" class="service-img" data-index="3" alt="nail art">
            <img src="/images/extensions-service.JPEG" class="service-img" data-index="4" alt="nail extensions">
        </div>
        <div class="service-detail-div">
            <div class="service-detail inset-border">
                .
            </div>
        </div>
    </div>
</section>

<style>
    .custom-button {
        border: 2px solid black;
        border-radius: 0.5rem;
        text-decoration: none;
        padding: 0.35rem 0.85rem;
        cursor: pointer;
        transition: filter 0.15s ease, transform 0.15s ease;
        display: inline-block;
    }

    .custom-button:hover {
        filter: brightness(0.95);
        transform: translateY(-1px);
    }

    .custom-button:active {
        transform: translateY(0);
        filter: brightness(0.9);
    }

    .services-div-first {
        width: 40vw;
    }

    .services-div-middle {
        display: flex;
        flex-direction: column;
        width: 28vw;
        gap: 1em;
    }

    .services-div-last {
        display: flex;
        flex-direction: column;
        width: 32vw;
        flex: 0 0 auto;
        justify-content: center;
        align-items: center;
    }

    .image-stack {
        position: relative;
        width: 60%;
        aspect-ratio: 1;
        margin: 1.25rem;
    }

    .service-img {
        position: absolute;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        object-fit: cover;
        border-radius: 3%;
    }

    .service-detail-div {
        width: 80%;
        overflow-wrap: break-word;
        padding: 0.4rem;
    }

    /* up to 6 width : 5 height ratio (most mobile) */
    @media (max-aspect-ratio: 6/5) {
        .custom-button {
            padding: 0.2rem 0.6rem;
        }

        .services-div-first {
            width: 90vw;
        }

        .services-div-middle {
            flex-direction: column;
            width: 100%;
            gap: 0.25em;
        }

        .services-div-last {
            flex-direction: row;
            width: 90vw;
        }
    }
</style>
