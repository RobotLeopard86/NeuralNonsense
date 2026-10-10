<script lang="ts">
	import Card from "../components/Card.svelte";
	import Logo from "../components/Logo.svelte";
	import SVGQR from "@svelte-put/qr/svg/QR.svelte";
	import { game } from "../lib/state.svelte";
	import { onMount } from "svelte";
	import { ejectIfNoConnection } from "../lib/connection.svelte";

	onMount(ejectIfNoConnection);

	let joinLink = $derived.by(() => "https://" + window.location.host + "/#/join/" + game.roomCode.toLowerCase());
</script>

<Logo />
<p class="text-center text-white/50 text-sm mb-8">Can you out-funny an AI?</p>

<main class="mb-auto w-full flex flex-col items-center px-4">
	<Card class="w-full">
		<p class="text-center text-white/50 text-md col-span-2">HOW TO JOIN</p>
		<div class="grid md:grid-rows-2 md:grid-cols-1 lg:grid-rows-1 lg:grid-cols-2 gap-5">
			<div class="flex-row items-center">
				<p class="text-left text-white font-bold text-md">Go to:</p>
				<p class="text-left text-white text-4xl font-bold min-w-0 wrap-break-word">
					{window.location.host}
				</p>
				<br />
				<p class="text-left text-white font-bold text-md">Use room code:</p>
				<p class="text-left text-white text-4xl font-bold mb-8 tracking-[1em]">
					{game.roomCode}
				</p>
			</div>
			<div class="flex-row items-center">
				<p class="text-center text-white font-bold text-md">Or, scan me:</p>
				<div class="flex-row justify-items-center">
					<div class="rounded-2xl w-1/2 bg-white border-black border-5">
						<SVGQR data={joinLink} class="inset-3" />
					</div>
				</div>
			</div>
		</div>
	</Card>
</main>
