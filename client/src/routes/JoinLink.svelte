<script lang="ts">
	import { ArrowRightOutline } from "flowbite-svelte-icons";

	import Logo from "../components/Logo.svelte";
	import NeonButton from "../components/NeonButton.svelte";
	import TextField from "../components/TextField.svelte";
    import { onMount } from "svelte";
    import { joinGame, rejoinIfConnected } from "../lib/connection.svelte";

	let { params = {} } = $props();
	let memberName = $state("");
	let roomCode = $derived.by(() => params.code.toUpperCase());

	onMount(rejoinIfConnected);

	const join = async () => {
		joinGame(roomCode, memberName);
	};
</script>

<Logo />
<p class="text-center text-white/50 text-sm mb-8">Can you out-funny an AI?</p>

<main class="mb-auto w-full flex flex-col items-center px-4">
	<div class="flex flex-col max-w-md w-full">
		<div class="gap-1">
			<p class="text-center text-white text-2xl font-bold">You"ve Been Invited!</p>
			<p class="text-center text-white text-lg font-medium">Joining game {roomCode}</p>
		</div>
		<br />
		<p class="text-left text-white/50 text-md mb-2">YOUR NAME</p>
		<TextField placeholder="What will you call yourself?" bind:value={memberName} class="w-full max-w-md" maxLength={32} />
		<br />
		<NeonButton
			variant="Yellow"
			onclick={join}
			class="w-full max-w-md my-4"
			disabled={!memberName}>Join Game <ArrowRightOutline size="xl" /></NeonButton
		>
	</div>
</main>
