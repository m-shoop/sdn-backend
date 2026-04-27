<script lang="ts">
  import NavigationBar from '../../NavigationBar.svelte';
  import Footer from '../../Footer.svelte';
  import type { PageData } from './$types';
  export let data;
</script>

<NavigationBar />
<section class="panel light-green-bg">
{#if (data.status == 200)}
  <h3>Your appointment has been confirmed!</h3>
{:else}
  <h3>The confirmation failed. {data.status}</h3>
{/if}
</section>
<Footer />