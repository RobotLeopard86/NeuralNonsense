<script lang="ts">
    import { onMount } from "svelte";
    import { ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
    import LoadingSpinner from "../components/LoadingSpinner.svelte";
    import { game, MemberType } from "../lib/state.svelte";
    import SmallNeonButton from "../components/SmallNeonButton.svelte";
    import { OpenDoorOutline } from "flowbite-svelte-icons";
    import RingTimer from "../components/RingTimer.svelte";
    import type { AnswerPhaseData } from "../lib/phase";

    //onMount(ejectIfNoConnection);

    let answer = $state("");
</script>

<main class="mb-auto w-full flex flex-col items-center px-4">
    {#if game.self.type == MemberType.Spectator}
        <header class="absolute h-10 inset-x-0 top-4 w-full">
            <!-- Leave button and round indicator -->
            <div class="absolute top-0 left-4 flex flex-col items-center w-1/5">
                <SmallNeonButton variant="Red" onclick={leaveRoom} class="mb-2 min-w-full"><OpenDoorOutline size="lg" />&nbsp;&nbsp;Leave Room</SmallNeonButton>
                <div class="px-4 py-1 min-w-full rounded-2xl font-bold text-sm flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
                    Round {game.state.round} | {game.self.score} pts
                </div>
            </div>

            <!-- Timer -->
            <div class="absolute top-0 right-4">
                <RingTimer size={72} stroke={4} duration={60000} />
            </div>
        </header>

        <!-- Body zone -->
        <main class="mb-auto mt-16 w-full h-[90dvh] flex flex-col items-center px-4">
            <!-- Prompt -->
            <h1 class="text-white font-black text-4xl text-center mb-8">{((game.phase ? game.phase : {}) as AnswerPhaseData).prompt || "An amazing prompt"}</h1>

            <!-- Response area -->
            <textarea placeholder="Respond to the prompt!" rows={3} maxlength={((game.phase ? game.phase : {}) as AnswerPhaseData).maxAnswerLength || 200} bind:value={answer}
                class="w-full bg-white/5 rounded-2xl p-4 text-white text-lg border-2 border-white/10 focus:border-[#FF2D78] focus:outline-none resize-none transition-colors">
            </textarea>
            <p class="w-full text-right text-white/50 text-sm mr-4">{answer.length}/{((game.phase ? game.phase : {}) as AnswerPhaseData).maxAnswerLength || 200}</p>
                
        </main>
    {:else}
        <!-- Leave button and round indicator -->
        <div class="absolute top-4 left-4 flex flex-col items-center w-1/5">
            <SmallNeonButton variant="Red" onclick={leaveRoom} class="mb-2 min-w-full"><OpenDoorOutline size="lg" />&nbsp;&nbsp;Leave Room</SmallNeonButton>
            <div class="px-4 py-1 min-w-full rounded-2xl font-bold text-sm flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
                Round {game.state.round}
            </div>
        </div>

        <!-- Timer -->
        <div class="absolute top-4 right-4">
            <RingTimer size={72} stroke={4} duration={60000} />
        </div>

        <!-- TODO: response statuses -->
    {/if}
</main>