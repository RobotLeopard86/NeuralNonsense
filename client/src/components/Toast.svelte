<script lang="ts">
    import { CloseOutline } from "flowbite-svelte-icons";
    import { fade, fly } from "svelte/transition";

    interface Props {
        message: string,
        duration: number,
		done: boolean
    }

    let { done = $bindable(), ...props }: Props = $props();

	let timeRemaining = $state(0);
	$effect(() => {
		timeRemaining = props.duration;
		const interval = setInterval(() => {
			timeRemaining -= 10;
			if(timeRemaining <= 0) {
				done = true;
				clearInterval(interval);
			}
		}, 10);
		return () => clearInterval(interval);
	});

	let progress: number = $derived(Math.min(1, Math.max(0, timeRemaining / props.duration)));
</script>

{#if timeRemaining > 0}
	<div class="rounded-2xl bg-[#FF2D78] shadow-[0_0_5px_rgba(255,45,120,0.4)] flex flex-row-reverse items-center px-4 py-2 z-50" in:fly={{ duration: 1000, x: 1000 }} out:fade={{ duration: 1000 }}>
		<span class="text-white text-md text-right">{props.message}</span>
		<span class="mx-2"></span>
		<button onclick={() => timeRemaining = 0}><CloseOutline size="sm" class="text-white" /></button>
	</div>
	<br/>
{/if}