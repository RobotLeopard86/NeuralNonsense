<script lang="ts">
    import { onMount } from "svelte";
    import { connection, ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
    import { game, MemberType } from "../lib/state.svelte";
    import { fade, fly } from "svelte/transition";
    import { OpenDoorOutline } from "flowbite-svelte-icons";
    import SmallNeonButton from "../components/SmallNeonButton.svelte";

    onMount(ejectIfNoConnection);

	interface Slide {
        title: string,
        body: string,
        body2: string
    }
	let currentSlide: Slide = $state({title: "", body: "", body2: ""});

	onMount(() => {
		switch(game.state.round) {
			case 2:
				currentSlide = {
					title: "Welcome to Round 2: Double Down",
					body: "The points you earn this round are doubled compared to the last.",
					body2: "Are you ready?",
				};
				break;
			case 3:
				currentSlide = {
					title: "It's Time for Round 3: Neural Showdown",
					body: "Now, everyone faces the same prompt, and your answers go up all at once!",
					body2: (game.self.type === MemberType.Player || game.self.type === MemberType.Audience) ? "You'll pick multiple answers this round; choose wisely!" : "Let's see how this shakes things up...",
				};
				break;
			default:
				currentSlide = {
					title: "Hang tight!",
					body: "The next round will start momentarily...",
					body2: "Thank you for your patience.",
				};
				break;
		}
		setTimeout(async() => {
            await connection().invoke("SignalReady");
        }, 5000);
	});

	let ready = $state(false);
	onMount(() => ready = true);
</script>

<!-- Leave button -->
{#if game.self.type !== MemberType.Projector}
	<SmallNeonButton variant="Red" onclick={leaveRoom} class="absolute top-4 left-4"><OpenDoorOutline size="lg" />&nbsp;&nbsp;Leave Room</SmallNeonButton>
{/if}

{#if ready}
<main in:fly={{ delay: 500, duration: 500, x: 0, y: 100 }} out:fly={{ duration: 500, x: 0, y: -100 }}  class="w-full min-h-full flex flex-col place-content-center px-4">
    <!-- Message -->
    <div class="grid">
        {#key currentSlide}
            <div class="layer">
                <p class="text-white font-bold text-4xl text-center mb-4" in:fade={{ delay: 500, duration: 500 }} out:fade={{ duration: 500 }}>{currentSlide.title}</p>
                <p class="text-white/50 text-lg text-center" in:fade={{ delay: 500, duration: 500 }} out:fade={{ duration: 500 }}>{currentSlide.body}</p>
                <p class="text-white/50 text-lg text-center" in:fade={{ delay: 500, duration: 500 }} out:fade={{ duration: 500 }}>{currentSlide.body2}</p>
            </div>
        {/key}
    </div>
</main>
{/if}