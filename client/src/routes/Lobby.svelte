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
	<!-- How to join -->
	<Card class="w-full">
		<p class="text-center text-white/50 text-md col-span-2">HOW TO JOIN</p>
		<div class="grid grid-rows-2 grid-cols-1 lg:grid-rows-1 lg:grid-cols-2 gap-5">
			<div class="flex-row items-center">
				<p class="text-left text-white font-bold text-md">Go to:</p>
				<p class="text-left text-white text-4xl font-bold min-w-0 wrap-break-word">
					{window.location.host}
				</p>
				<br />
				<p class="text-left text-white font-bold text-md">Use room code:</p>
				<p class="text-left text-white text-4xl font-bold mb-8 tracking-[0.5em] lg:tracking-[1em] wrap-break-word">
					{game.roomCode}
				</p>
			</div>
			<div class="flex-row place-items-center">
				<p class="text-left lg:text-center text-white font-bold text-md">Or, scan me:</p>
				<div class="rounded-2xl w-1/2 bg-white border-black border-5">
					<SVGQR data={joinLink} class="inset-3" />
				</div>
			</div>
		</div>
	</Card>
	<br />

	<!-- Players list -->
	<Card class="w-full flex flex-col items-center">
		<p class="text-center text-white/50 text-md col-span-2 mb-2">{game.state.members.length}/8 PLAYERS</p>
		{#each game.state.members as member}
			<Card class="w-11/12 mx-auto mb-2">
				<span class="text-left text-white font-bold">{member.name}
					{#if member.uuid == game.self.uuid}	
						<span class="text-[#00E5FF] rounded-4xl bg-[#00E5FF]/25 px-4 py-1 mx-4">YOU</span>
						{#if member.uuid == game.state.hostID}	
							<span class="text-[#FF2D78] rounded-4xl bg-[#FF2D78]/25 px-4 py-1">HOST</span>
						{/if}
					{:else if member.uuid == game.state.hostID}	
						<span class="text-[#FF2D78] rounded-4xl bg-[#FF2D78]/25 px-4 py-1 mx-4">HOST</span>
					{/if}
				</span>
			</Card>
		{/each}
	</Card>
</main>
