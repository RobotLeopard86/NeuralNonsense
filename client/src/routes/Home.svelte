<script lang="ts">
	import { fly } from 'svelte/transition';
	import { ArrowLeftOutline, ArrowRightOutline } from 'flowbite-svelte-icons';

	import Logo from '../components/Logo.svelte';
	import NeonButton from '../components/NeonButton.svelte';
	import TextField from '../components/TextField.svelte';

	type Phase = 'Initial' | 'Join' | 'Create';
	let phase: Phase = $state('Initial');

	let playerName = $state('');
	let roomCode = $state('');
	$effect(() => {
		roomCode = roomCode.toUpperCase();
	});

	let canJoin = $derived(playerName && roomCode && roomCode.length === 4);
</script>

<Logo />
<p class="text-center text-white/50 text-sm mb-8">Can you out-funny an AI?</p>

<main class="mb-auto w-full flex flex-col items-center px-4">
	{#if phase == 'Initial'}
		<NeonButton variant="Cyan" onclick={() => (phase = 'Join')} class="w-full max-w-md my-4">Join an Existing Game</NeonButton>
		<NeonButton variant="Red" onclick={() => (phase = 'Create')} class="w-full max-w-md my-4">Create a New Game</NeonButton>
	{:else if phase == 'Join'}
		<div class="flex flex-col max-w-md w-full">
			<button class="text-left text-white/50 text-sm mb-8 flex flex-row hover:text-white/75 transition-colors" onclick={() => (phase = 'Initial')}><ArrowLeftOutline size="md" /> Back</button>
			<p class="text-left text-white/50 text-md mb-2">YOUR NAME</p>
			<TextField placeholder="What will you call yourself?" bind:value={playerName} class="w-full max-w-md" maxLength={20} />
			<br />
			<p class="text-left text-white/50 text-md mb-2">ROOM CODE</p>
			<input type="text" maxlength="4" placeholder="CODE" bind:value={roomCode} class="w-full max-w-md focus:outline-none px-8 py-4 rounded-2xl text-lg bg-white/5 border-2 border-white/10 text-white placeholder:text-white/30 font-bold text-center tracking-[1em] placeholder:tracking-normal placeholder:font-normal" />
			<br />
			<NeonButton
				variant="Cyan"
				onclick={() => {
					alert(`You joined game ${roomCode} as ${playerName}!`);
				}}
				class="w-full max-w-md my-4"
				disabled={!canJoin}>Join Game <ArrowRightOutline size="xl" /></NeonButton>
		</div>
	{:else if phase == 'Create'}
		<div class="flex flex-col max-w-md w-full">
			<button class="text-left text-white/50 text-sm mb-8 flex flex-row hover:text-white/75 transition-colors" onclick={() => (phase = 'Initial')}><ArrowLeftOutline size="md" /> Back</button>
			<p class="text-left text-white/50 text-md mb-2">YOUR NAME</p>
			<TextField placeholder="What will you call yourself?" bind:value={playerName} class="w-full max-w-md" maxLength={20} />
			<br />
			<NeonButton
				variant="Red"
				onclick={() => {
					alert(`You created a new game as ${playerName}!`);
				}}
				class="w-full max-w-md my-4"
				disabled={!playerName}>Create <ArrowRightOutline size="xl" /></NeonButton>
		</div>
	{/if}
</main>

<footer class="h-10">
	<p class="text-center text-white/50 text-sm mb-8">&#169; 2026 Owen Z. Siebers. <a href="https://github.com/RobotLeopard86/NeuralNonsense"><u>Source code</u></a> available under the PolyForm Perimeter License 1.0.1.</p>
</footer>
