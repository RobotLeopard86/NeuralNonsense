<script lang="ts">
	import { ArrowLeftOutline, ArrowRightOutline } from "flowbite-svelte-icons";

	import Logo from "../components/Logo.svelte";
	import NeonButton from "../components/NeonButton.svelte";
	import TextField from "../components/TextField.svelte";
	import { joinRoom, tryRestore } from "../lib/connection.svelte";
	import { onMount } from "svelte";
    import { addToast } from "../lib/toast.svelte";
    import LoadingSpinner from "../components/LoadingSpinner.svelte";

	type Phase = "Initial" | "Join" | "Create";
	let phase: Phase = $state("Initial");

	let memberName = $state("");
	let roomCode = $state("");
	$effect(() => {
		roomCode = (roomCode.toUpperCase().match(/[A-Z]*/) || []).join("") || "";
	});

	let canJoin = $derived(memberName && roomCode && roomCode.length === 6);
	let working = $state(false);

	onMount(tryRestore);

	const join = async () => {
		working = true;
		await joinRoom(roomCode, memberName);
	};

	const create = async () => {
		//Call room creation endpoint
		working = true;
		const result = await fetch("/portal/create", {
			method: "POST",
		});

		//Alert if it went wrong
		if (!result.ok) {
			addToast("Failed to create room!", 5000);
			return;
		}

		//Set room code and proceed with join flow
		const body = await result.json();
		roomCode = body.code;
		await join();
	};
</script>

<Logo />
<p class="text-center text-white/50 text-sm mb-8">Can you out-funny an AI?</p>

<main class="mb-auto w-full flex flex-col items-center px-4">
	{#if phase == "Initial"}
		<NeonButton variant="Cyan" onclick={() => (phase = "Join")} class="w-full max-w-md my-4">Join an Existing Room</NeonButton>
		<NeonButton variant="Red" onclick={() => (phase = "Create")} class="w-full max-w-md my-4">Create a New Room</NeonButton>
	{:else if phase == "Join"}
		<div class="flex flex-col max-w-md w-full">
			<button class="text-left text-white/50 text-sm mb-8 flex flex-row hover:text-white/75 transition-colors" onclick={() => (phase = "Initial")}
				><ArrowLeftOutline size="md" /> Back</button
			>
			<p class="text-left text-white/50 text-base mb-2">ROOM CODE</p>
			<input
				type="text"
				maxlength="6"
				placeholder="CODE"
				bind:value={roomCode}
				class="w-full max-w-md focus:outline-none px-8 py-4 rounded-2xl text-lg bg-white/5 border-2 border-white/10 text-white placeholder:text-white/30 font-bold text-center tracking-[1em] placeholder:font-normal"
			/>
			<br />
			<p class="text-left text-white/50 text-base mb-2">YOUR NAME</p>
			<TextField placeholder="What will you call yourself?" bind:value={memberName} class="w-full max-w-md" maxLength={32} />
			<br />
			<NeonButton variant="Cyan" onclick={join} class="w-full max-w-md my-4" disabled={!canJoin || working}>
				{#if working}
					<LoadingSpinner size={25} stroke={2} minDegrees={1} maxDegrees={120} />
					<span>&nbsp;&nbsp;&nbsp;&nbsp;</span>
					<p class="text-center text-lg">Joining...</p>
				{:else}
					Join Game <ArrowRightOutline size="xl" />
				{/if}
			</NeonButton>
		</div>
	{:else if phase == "Create"}
		<div class="flex flex-col max-w-md w-full">
			<button class="text-left text-white/50 text-sm mb-8 flex flex-row hover:text-white/75 transition-colors" onclick={() => (phase = "Initial")}
				><ArrowLeftOutline size="md" /> Back</button
			>
			<p class="text-left text-white/50 text-base mb-2">YOUR NAME</p>
			<TextField placeholder="What will you call yourself?" bind:value={memberName} class="w-full max-w-md" maxLength={32} />
			<br />
			<NeonButton variant="Red" onclick={create} class="w-full max-w-md my-4" disabled={!memberName || working}>
				{#if working}
					<LoadingSpinner size={25} stroke={2} minDegrees={1} maxDegrees={120} />
					<span>&nbsp;&nbsp;&nbsp;&nbsp;</span>
					<p class="text-center text-lg">Creating...</p>
				{:else}
					Create <ArrowRightOutline size="xl" />
				{/if}
			</NeonButton>
		</div>
	{/if}
</main>
