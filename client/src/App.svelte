<script lang="ts">
	import Router from "svelte-spa-router";
	import routes from "./routes";
    import Toast from "./components/Toast.svelte";
    import { toasts } from "./lib/toast.svelte";
    import { game, MemberType } from "./lib/state.svelte";
</script>

<div class="relative min-h-dvh bg-[#1A1035] overflow-hidden">
	<div
		class="absolute inset-0 pointer-events-none"
		style="
      background:
        radial-gradient(circle at 20% 20%, rgba(255,45,120,0.18), transparent 50%),
        radial-gradient(circle at 80% 60%, rgba(0,229,255,0.14), transparent 50%),
        radial-gradient(circle at 50% 100%, rgba(255,230,0,0.10), transparent 50%);
    ">
	</div>

	<div class="absolute top-4 right-4 flex flex-col max-w-1/3 items-start align-bottom">
		{#each toasts.filter((t) => !t.done) as toast (toast.id)}
			<Toast message={toast.message} duration={toast.duration} bind:done={toast.done} />
		{/each}
	</div>

	{#if game.self.uuid && game.self.type === MemberType.Spectator}
	<header class="h-10 bg-amber-300/50 border-b-2 border-amber-400 border-solid flex flex-col">
		<p class="text-amber-300 text-base font-bold text-center mx-auto my-auto">You're Spectating</p>
	</header>
	{/if}

	<div class="relative z-10 min-h-[95dvh] flex flex-col justify-self-center items-center justify-center px-6 py-12 gap-5 lg:w-1/2 md:w-3/4 sm:w-full">
		<Router {routes} restoreScrollState={true} />
	</div>

	<footer class="h-10">
		<p class="text-center text-white/50 text-sm mb-8">
			&#169; 2026 Owen Z. Siebers. <a href="https://github.com/RobotLeopard86/NeuralNonsense" target="_blank"><u>Source code</u></a> available under the PolyForm Perimeter License 1.0.1.
		</p>
	</footer>
</div>
