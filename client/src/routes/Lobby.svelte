<script lang="ts">
	import Card from "../components/Card.svelte";
	import Logo from "../components/Logo.svelte";
	import SVGQR from "@svelte-put/qr/svg/QR.svelte";
	import { game, MemberType } from "../lib/state.svelte";
	import { onMount } from "svelte";
	import { connection, ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
	import NeonButton from "../components/NeonButton.svelte";
	import { ArrowRightOutline, EyeOutline, OpenDoorOutline, UserRemoveOutline } from "flowbite-svelte-icons";
    import SmallNeonButton from "../components/SmallNeonButton.svelte";
    import LoadingSpinner from "../components/LoadingSpinner.svelte";
    import { startGame } from "../lib/client";
    import { fly } from "svelte/transition";

	onMount(ejectIfNoConnection);

	let joinLink = $derived.by(() => "https://" + window.location.host + "/#/join/" + game.roomCode.toLowerCase());
	let canStart = $derived(game.state.members.filter((m) => m.type == MemberType.Player	).length >= 3);
	let started = $state(false);

	let copiedLink = $state(false);
	let copiedCode = $state(false);

	const copyLink = async() => {
		await navigator.clipboard.writeText("https://" + window.location.host);
		copiedLink = true;
		setTimeout(() => copiedLink = false, 3000);
	}

	const copyCode = async() => {
		await navigator.clipboard.writeText(game.roomCode);
		copiedCode = true;
		setTimeout(() => copiedCode = false, 3000);
	}
	
	const launchProjector = async() => {
		//Have the server create a projector slot
		let ptoken = await connection().invoke<string>("AddProjector");

		//Open the projector window with the token
		window.open(`/#/projector-launch/${ptoken}`, "_blank");
	}

	let ready = $state(false);
	onMount(() => ready = true);
</script>

<Logo />
<p class="text-center text-white/50 text-sm mb-8">Can you out-funny an AI?</p>

{#if ready}
<main in:fly={{ delay: 500, duration: 500, x: 0, y: 100 }} out:fly={{ duration: 500, x: 0, y: -100 }}  class="mb-auto w-full flex flex-col items-center px-4">
	<!-- Leave button -->
	{#if game.self.type !== MemberType.Projector}
		<SmallNeonButton variant="Red" onclick={leaveRoom} class="absolute top-4 left-4"><OpenDoorOutline size="lg" />&nbsp;&nbsp;Leave Room</SmallNeonButton>
	{/if}

	<!-- How to join -->
	<Card class="w-full">
		<p class="text-center text-white/50 text-base col-span-2">HOW TO JOIN</p>
		<div class="grid grid-rows-2 grid-cols-1 lg:grid-rows-1 lg:grid-cols-2 gap-5">
			<div class="flex-row items-center">
				<p class="text-left text-white font-bold text-base">Go to:</p>
				<p class="text-left text-white text-4xl font-bold min-w-0 wrap-break-word">{window.location.host}</p>
				{#if game.self.type !== MemberType.Projector}
					{#if copiedLink}
						<button class="text-left text-[#00E5FF] font-semibold text-sm">Copied to clipboard!</button>
					{:else}
						<button class="text-left text-white/50 underline text-sm cursor-pointer" onclick={copyLink}>Copy link</button>
					{/if}
				{/if}
				<br />
				<br />
				<p class="text-left text-white font-bold text-base">Use room code:</p>
				<p class="text-left text-white text-4xl font-bold tracking-[0.5em] lg:tracking-[0.9em] wrap-break-word">{game.roomCode}</p>
				{#if game.self.type !== MemberType.Projector}
					{#if copiedCode}
						<button class="text-left text-[#00E5FF] font-semibold text-sm">Copied to clipboard!</button>
					{:else}
						<button class="text-left text-white/50 underline text-sm cursor-pointer" onclick={copyCode}>Copy game code</button>
					{/if}
				{/if}
			</div>
			<div class="flex-row place-items-center">
				<p class="text-left lg:text-center text-white font-bold text-base">Or, scan me:</p>
				<div class="rounded-2xl w-1/2 bg-white border-black border-5">
					<SVGQR data={joinLink} class="inset-3" />
				</div>
			</div>
		</div>
	</Card>
	<br />

	<!-- Players list -->
	<Card class="w-full flex flex-col items-center">
		<p class="text-center text-white/50 text-base col-span-2 mb-2">{game.state.members.filter(m => m.type === MemberType.Player).length}/8 PLAYERS</p>
		{#each game.state.members.filter(m => m.type === MemberType.Player) as member}
			<Card class="w-11/12 mx-auto mb-2 flex flex-row items-center">
				<span class="text-left text-white font-bold">
					{member.name}
					{#if member.uuid == game.self.uuid}
						<span class="text-[#00E5FF] rounded-4xl bg-[#00E5FF]/25 px-4 py-1 mx-4">YOU</span>
						{#if member.uuid == game.state.hostID}
							<span class="text-[#FF2D78] rounded-4xl bg-[#FF2D78]/25 px-4 py-1">HOST</span>
						{/if}
					{:else if member.uuid == game.state.hostID}
						<span class="text-[#FF2D78] rounded-4xl bg-[#FF2D78]/25 px-4 py-1 mx-4">HOST</span>
					{/if}
				</span>
				{#if game.self.uuid == game.state.hostID && member.uuid != game.self.uuid}
				<span class="mx-auto"></span>
				<SmallNeonButton variant="Red" onclick={async() => {
							await connection().invoke("KickUser", member.uuid);
						}}><UserRemoveOutline size="lg"/>&nbsp;&nbsp;Kick</SmallNeonButton>
				{/if}
			</Card>
		{/each}
		{#if game.self.type === MemberType.Audience}
			<p class="text-center text-white text-base font-bold">You're in the audience!</p>
		{/if}
	</Card>
	<br />

	<!-- Projector view launch button -->
	{#if game.self.uuid == game.state.hostID}
		<NeonButton variant="Translucent" onclick={launchProjector} class="w-full my-4">
			<EyeOutline size="xl" />&nbsp;&nbsp;Launch Projector View (New Tab)
		</NeonButton>
	{/if}

	<!-- Start button -->
	{#if game.self.uuid == game.state.hostID}
		<NeonButton variant="Yellow" onclick={() => {
			startGame();
			started = true;
		}} disabled={!canStart || started} class="w-full my-4">
			{#if started}
				<LoadingSpinner size={25} stroke={2} minDegrees={1} maxDegrees={120} />
				<span>&nbsp;&nbsp;&nbsp;&nbsp;</span>
				<p class="text-center text-lg">Starting...</p>
			{:else}
				{#if game.state.members.length < 3}
					Need 3 Players to Start
				{:else}
					Start Game <ArrowRightOutline size="xl" />
				{/if}
			{/if}
		</NeonButton>
	{:else}
		<div class="flex flex-row items-center">
			<LoadingSpinner size={25} stroke={2} minDegrees={1} maxDegrees={120} />
			<span>&nbsp;&nbsp;&nbsp;&nbsp;</span>
			<p class="text-center text-white/50 text-sm">Waiting for the host to start the game...</p>
		</div>
	{/if}
</main>
{/if}
