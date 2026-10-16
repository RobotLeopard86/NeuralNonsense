<script lang="ts">
    import { onMount } from "svelte";
    import { connection, ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
    import LoadingSpinner from "../components/LoadingSpinner.svelte";
    import { game, MemberType } from "../lib/state.svelte";
    import { fade } from "svelte/transition";
    import { OpenDoorOutline } from "flowbite-svelte-icons";
    import SmallNeonButton from "../components/SmallNeonButton.svelte";

    //onMount(ejectIfNoConnection);

    interface Slide {
        title: string,
        body: string,
        body2: string
    }

    const slideTime = 5000;
    const pslides: Slide[] = [
        {
            title: "Read the Prompt",
            body: "You'll see a prompt on your device. Try to respond in a funny, clever, or absurd way.",
            body2: "Everyone gets a different prompt, and you'll have 60 seconds to answer."
        },
        {
            title: "The People Decide",
            body: "Everybody's answer goes up against an AI's response to the same prompt.",
            body2: "The authors are hidden, so pick your favorite response!"
        },
        {
            title: "3 Rounds. One Winner.",
            body: "You'll earn points each round depending on how well your answer does.",
            body2: "Stack 'em up across all 3 rounds to take the crown."
        },
        {
            title: "Hang Tight!",
            body: "We're cooking up some fresh AI content, just for you!",
            body2: "The game will begin momentarily..."
        }
    ];
    const aslides: Slide[] = [
        {
            title: "You're in the Audience",
            body: "Players will receive a prompt and respond in a funny, clever, or absurd way.",
            body2: "Everyone gets a different prompt, and they'll have 60 seconds to answer."
        },
        {
            title: "The People Decide",
            body: "Everybody's answer goes up against an AI's response to the same prompt.",
            body2: "The authors are hidden, so pick your favorite response!"
        },
        {
            title: "3 Rounds. One Winner.",
            body: "Players earn points each round depending on how well their answers do.",
            body2: "The player with the most points after all 3 rounds wins!"
        },
        {
            title: "Hang Tight!",
            body: "We're cooking up some fresh AI content, just for you!",
            body2: "The game will begin momentarily..."
        }
    ];
	const sslides: Slide[] = [
        {
            title: "Welcome to Neural Nonsense!",
            body: "Players will receive a prompt and respond in a funny, clever, or absurd way.",
            body2: "Everyone gets a different prompt, and they'll have 60 seconds to respond."
        },
        {
            title: "The People Decide",
            body: "Everybody's answer goes up against an AI's response to the same prompt.",
            body2: "Players and audience members vote for their favorite."
        },
        {
            title: "3 Rounds. One Winner.",
            body: "Players earn points each round depending on how well their answers do.",
            body2: "The player with the most points after all 3 rounds wins!"
        },
        {
            title: "Hang Tight!",
            body: "We're cooking up some fresh AI content, just for you!",
            body2: "The game will begin momentarily..."
        }
    ];
    const slides: Slide[] = game.self.type === MemberType.Player ? pslides : (game.self.type === MemberType.Audience ? aslides : sslides);

    let slideIndex = $state(0);
    let currentSlide: Slide = $derived(slides[slideIndex]);
    $effect(() => {
        const interval = setInterval(() => {
			slideIndex++;
		}, slideTime);
		setTimeout(async() => {
            clearInterval(interval);
            await connection().invoke("SignalReady");
        }, slideTime * (slides.length - 1));
		return () => clearInterval(interval);
    });
</script>

<main class="w-full min-h-full flex flex-col place-content-center px-4">
    <!-- Leave button -->
	{#if game.self.type !== MemberType.Projector}
		<SmallNeonButton variant="Red" onclick={leaveRoom} class="absolute top-4 left-4"><OpenDoorOutline size="lg" />&nbsp;&nbsp;Leave Room</SmallNeonButton>
	{/if}

    <!-- Spinner and slides -->
    <LoadingSpinner size={150} stroke={7} minDegrees={1} maxDegrees={120} />
    <br/>
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

<style scoped>
    .layer {
        grid-area: 1 / 1;
    }
</style>