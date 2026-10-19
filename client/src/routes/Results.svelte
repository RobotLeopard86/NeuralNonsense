<script lang="ts">
    import { onMount } from "svelte";
    import { ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
    import { game, MemberType } from "../lib/state.svelte";
    import SmallNeonButton from "../components/SmallNeonButton.svelte";
    import { OpenDoorOutline, UserOutline } from "flowbite-svelte-icons";
    import type { ResultsPhaseData } from "../lib/phase";

    onMount(ejectIfNoConnection);
</script>

<main class="w-full min-h-full flex flex-col items-center px-4">
	<!-- Leave button -->
	{#if game.self.type !== MemberType.Projector}
		<SmallNeonButton variant="Red" onclick={leaveRoom} class="absolute top-4 left-4"><OpenDoorOutline size="lg" />&nbsp;&nbsp;Leave Room</SmallNeonButton>
	{/if}

	<!-- Score indicator -->
    {#if game.self.type === MemberType.Player || game.self.type === MemberType.Audience}
		<div class="absolute top-4 right-4 px-4 py-1 rounded-2xl font-bold text-sm flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
			{game.self.score} Points
		</div>
	{/if}
        
	<!-- Body zone -->
	<div class="w-3/4 min-h-[85dvh] flex flex-col place-content-center px-4">
		<!-- Header -->
		<div class="w-full flex flex-row items-center mb-25">
			<!-- Round indicator -->
			<span class="px-4 py-1 rounded-2xl w-1/7 font-bold text-lg flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
				Round {game.state.round}
			</span>
		</div>

		<!-- Prompt -->
		<p class="text-white/50 text-base text-left mb-2">THE PROMPT WAS:</p>
		<p class="text-white font-black text-4xl text-left mb-4 pb-6">{((game.phase ? game.phase : {}) as ResultsPhaseData).prompt || "An amazing prompt"}</p>

		<!-- Results -->
		<div class="px-8 py-4 rounded-2xl font-normal text-lg bg-white/5 border-2 border-white/20 text-white flex flex-col mb-8 items-start w-full">
			<div class="flex flex-row justify-start items-center w-full mb-4 text-white/50 text-sm">
				<span class="bg-[#FF2D78] text-white font-bold text-sm px-3 py-1 rounded-full flex flex-row items-center"><UserOutline size="md" />&nbsp;HUMAN</span>
				&nbsp;&nbsp;&nbsp;
				{((game.phase ? game.phase : {}) as ResultsPhaseData).humanPlayer || "Someone"}
			</div>
			{((game.phase ? game.phase : {}) as ResultsPhaseData).humanAnswer || "What an incredible human answer!"}
			<div class="flex flex-row justify-start items-center w-full">
				<div class="w-11/12 rounded-full h-2 bg-white/10">
					<div class="rounded-full h-2 bg-[#FF2D78]" style={`width: ${(((game.phase ? game.phase : {}) as ResultsPhaseData).humanPoints || game.state.round * 10) / (game.state.round * 10)}%`}></div>
				</div>
				&nbsp;&nbsp;&nbsp;
				<span class="text-[#FF2D78] text-4xl font-bold">+{((game.phase ? game.phase : {}) as ResultsPhaseData).humanPoints || game.state.round * 10}</span>
			</div>
		</div>
		<div class="px-8 py-4 rounded-2xl font-normal text-lg bg-white/5 border-2 border-white/20 text-white flex flex-col mb-8 justify-start items-start w-full">
			<div class="flex flex-row justify-start items-center w-full mb-4">
				<span class="bg-[#00E5FF] text-[#1A1035] font-bold text-sm px-8 py-1 rounded-full flex flex-row items-center"><UserOutline size="md" />&nbsp;AI</span>
			</div>
			{((game.phase ? game.phase : {}) as ResultsPhaseData).humanAnswer || "What an incredible robot answer!"}
		</div>
	</div>
</main>