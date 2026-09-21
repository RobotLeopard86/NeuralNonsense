<script lang="ts">
	import { ArrowLeftOutline, ArrowRightOutline } from 'flowbite-svelte-icons';

	import Logo from '../components/Logo.svelte';
	import NeonButton from '../components/NeonButton.svelte';
	import TextField from '../components/TextField.svelte';

	let { params = {} } = $props();
	let playerName = $state('');
	let roomCode = $derived.by(() => params.code.toUpperCase());
</script>

<Logo />
<p class="text-center text-white/50 text-sm mb-8">Can you out-funny an AI?</p>

<main class="mb-auto w-full flex flex-col items-center px-4">
	<div class="flex flex-col max-w-md w-full">
		<div class="gap-1">
			<p class="text-center text-white text-2xl font-bold">You've Been Invited!</p>
			<p class="text-center text-white text-lg font-medium">Joining game {roomCode}</p>
		</div>
		<br />
		<p class="text-left text-white/50 text-md mb-2">YOUR NAME</p>
		<TextField placeholder="What will you call yourself?" bind:value={playerName} class="w-full max-w-md" maxLength={20} />
		<br />
		<NeonButton
			variant="Yellow"
			onclick={() => {
				alert(`You joined game ${roomCode} as ${playerName}!`);
			}}
			class="w-full max-w-md my-4"
			disabled={!playerName}>Join Game <ArrowRightOutline size="xl" /></NeonButton>
	</div>
</main>
