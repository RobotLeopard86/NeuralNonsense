<script lang="ts">
	import { fly } from 'svelte/transition';
	import { ArrowLeftOutline } from 'flowbite-svelte-icons';

	import Logo from '../components/Logo.svelte';
	import NeonButton from '../components/NeonButton.svelte';

	type Phase = 'Initial' | 'Code' | 'Name';
	let phase: Phase = $state('Initial');
</script>

<Logo />
<p class="text-center text-white/50 text-sm mb-8">Can you out-funny an AI?</p>

<main class="mb-auto grid">
	{#if phase == 'Initial'}
		<div in:fly={{ duration: 500, x: 1000 }} out:fly={{ duration: 500, x: -1000 }} class="[grid-area:1/1]">
			<NeonButton variant="Cyan" onclick={() => (phase = 'Code')} class="w-full m-4">Join an Existing Game</NeonButton>
			<NeonButton variant="Red" onclick={() => (phase = 'Name')} class="w-full m-4">Create a New Game</NeonButton>
		</div>
	{:else if phase == 'Code'}
		<div in:fly={{ duration: 500, x: 1000 }} out:fly={{ duration: 500, x: -1000 }} class="[grid-area:1/1]">
			<NeonButton variant="Translucent" onclick={() => (phase = 'Initial')} class="w-full m-4"><ArrowLeftOutline size="xl" /> Back</NeonButton>
		</div>
	{:else if phase == 'Name'}
		<div in:fly={{ duration: 500, x: 1000 }} out:fly={{ duration: 500, x: -1000 }} class="[grid-area:1/1]">
			<NeonButton variant="Translucent" onclick={() => (phase = 'Initial')} class="w-full m-4"><ArrowLeftOutline size="xl" /> Back</NeonButton>
		</div>
	{/if}
</main>

<footer class="h-10">
	<p class="text-center text-white/50 text-sm mb-8">&#169; 2026 Owen Z. Siebers. <a href="https://github.com/RobotLeopard86/NeuralNonsense"><u>Source code</u></a> available under the PolyForm Perimeter License 1.0.1.</p>
</footer>
