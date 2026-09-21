<script lang="ts">
	import Card from '../components/Card.svelte';
	import Logo from '../components/Logo.svelte';
	import SvgQR from '@svelte-put/qr/svg/QR.svelte';

	let roomCode = $state('TEMP');
	let joinLink = $derived.by(() => 'https://' + window.location.host + '/#/join/' + roomCode.toLowerCase());
</script>

<Logo />
<p class="text-center text-white/50 text-sm mb-8">Can you out-funny an AI?</p>

<main class="mb-auto w-full flex flex-col items-center px-4">
	<Card class="w-full">
		<p class="text-center text-white/50 text-md col-span-2">HOW TO JOIN</p>
		<div class="grid grid-rows-1 grid-cols-2">
			<div class="flex-row items-center">
				<p class="text-left text-white font-bold text-md">Go to:</p>
				<p class="text-left text-white text-4xl font-bold mb-8">{window.location.host}</p>
				<br />
				<p class="text-left text-white font-bold text-md">Use room code:</p>
				<p class="text-left text-white text-4xl font-bold mb-8 tracking-[1em]">{roomCode}</p>
			</div>
			<div class="flex-row items-center">
				<p class="text-center text-white font-bold text-md">Or, scan me:</p>
				<div class="rounded-2xl bg-white border-black border-5">
					<SvgQR data={joinLink} class="inset-3"></SvgQR>
				</div>
			</div>
		</div>
	</Card>
</main>
