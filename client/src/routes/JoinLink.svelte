<script lang="ts">
	import { ArrowRightOutline } from "flowbite-svelte-icons";
	import Logo from "../components/Logo.svelte";
	import NeonButton from "../components/NeonButton.svelte";
	import TextField from "../components/TextField.svelte";
	import { onMount } from "svelte";
	import { joinRoom, tryRestore } from "../lib/connection.svelte";
	import { push } from "svelte-spa-router";
    import { addToast } from "../lib/toast.svelte";
	import xss from "xss";
    import { fly } from "svelte/transition";

	let { params = {} } = $props();
	let memberName = $state("");
	let roomCode = $derived.by(() => params.code.toUpperCase());

	onMount(tryRestore);
	onMount(() => {
		if (roomCode.length != 6) {
			addToast("Invalid join link!", 5000);
			push("/");
		}
	});

	const join = async () => {
		await joinRoom(roomCode, xss(memberName));
	};

	let ready = $state(false);
	onMount(() => ready = true);
</script>

<Logo />
<p class="text-center text-white/50 text-sm mb-8">Can you out-funny an AI?</p>

{#if ready}
<main in:fly={{ delay: 500, duration: 500, x: 0, y: 100 }} out:fly={{ duration: 500, x: 0, y: -100 }}  class="mb-auto w-full flex flex-col items-center px-4">
	<div class="flex flex-col max-w-md w-full">
		<div class="gap-1">
			<p class="text-center text-white text-2xl font-bold">You've Been Invited!</p>
			<p class="text-center text-white text-lg font-medium">Joining game {roomCode}</p>
		</div>
		<br />
		<p class="text-left text-white/50 text-base mb-2">YOUR NAME</p>
		<TextField placeholder="What will you call yourself?" bind:value={memberName} class="w-full max-w-md" maxLength={32} />
		<br />
		<NeonButton variant="Yellow" onclick={join} class="w-full max-w-md my-4" disabled={!memberName}>Join Game <ArrowRightOutline size="xl" /></NeonButton>
	</div>
</main>
{/if}
