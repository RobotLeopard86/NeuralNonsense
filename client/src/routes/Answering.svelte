<script lang="ts">
    import { onMount } from "svelte";
    import { ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
    import LoadingSpinner from "../components/LoadingSpinner.svelte";
    import { game, MemberType } from "../lib/state.svelte";
    import { fade } from "svelte/transition";
    import SmallNeonButton from "../components/SmallNeonButton.svelte";
    import { OpenDoorOutline } from "flowbite-svelte-icons";
    import RingTimer from "../components/RingTimer.svelte";

    //onMount(ejectIfNoConnection);
</script>

<main class="mb-auto w-full flex flex-col items-center px-4">
    {#if game.self.type == MemberType.Spectator}
        <!-- Leave button and round indicator -->
        <div class="absolute top-4 left-4 flex flex-col items-center w-1/5">
            <SmallNeonButton variant="Red" onclick={leaveRoom} class="mb-2 min-w-full"><OpenDoorOutline size="lg" />&nbsp;&nbsp;Leave Room</SmallNeonButton>
            <div class="px-4 py-1 min-w-full rounded-2xl font-bold text-sm flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
                Round {game.state.round} | {game.self.score} pts
            </div>
        </div>

        <!-- Timer -->
        <div class="absolute top-4 right-4">
            <RingTimer size={72} stroke={4} duration={60000} />
        </div>

        <!-- Prompt -->
        <h1 class="text-white font-black text-4xl text-center">An amazing prompt will go here</h1>
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