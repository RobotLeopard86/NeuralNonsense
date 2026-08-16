<script lang="ts">
	import Router from 'svelte-spa-router';
	import routes from './routes';
</script>

<div class="relative min-h-screen bg-[#1A1035] overflow-hidden">
	<div
		class="absolute inset-0"
		style="
      background:
        radial-gradient(circle at 20% 20%, rgba(255,45,120,0.18), transparent 50%),
        radial-gradient(circle at 80% 60%, rgba(0,229,255,0.14), transparent 50%),
        radial-gradient(circle at 50% 100%, rgba(255,230,0,0.10), transparent 50%);
    ">
	</div>

	<div class="relative z-10 min-h-screen flex flex-col justify-self-center items-center justify-center px-6 py-12 gap-5 md:w-1/2 lg:w-1/3">
		<Router {routes} />
	</div>
</div>
