<script lang="ts">
    import { onMount } from "svelte";
    import { ejectIfNoConnection } from "../lib/connection.svelte";
    import LoadingSpinner from "../components/LoadingSpinner.svelte";

    //onMount(ejectIfNoConnection);

    interface Slide {
        title: string,
        body: string,
        body2: string
    }

    const slideTime = 5000;
    const slides: Slide[] = [
        {
            title: "Read the Prompt",
            body: "You'll see a prompt on your device. Try to respond in a funny, clever, or absurd way.",
            body2: "Everyone gets a different prompt, and you'll have 60 seconds to answer."
        },
        {
            title: "A Battle of Wits",
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

    let slideIndex = $state(0);
    let currentSlide: Slide = $derived(slides[slideIndex]);
    $effect(() => {
        const interval = setInterval(() => {
			slideIndex++;
		}, slideTime);
		setTimeout(() => clearInterval(interval), slideTime * (slides.length - 1));
		return () => clearInterval(interval);
    });
</script>

<main class="w-full min-h-screen flex flex-col place-content-center px-4">
    <LoadingSpinner size={150} stroke={7} minDegrees={1} maxDegrees={120} />
    <br/>
    <p class="text-white font-bold text-4xl text-center mb-4">{currentSlide.title}</p>
    <p class="text-white/50 text-lg text-center">{currentSlide.body}</p>
    <p class="text-white/50 text-lg text-center">{currentSlide.body2}</p>
</main>