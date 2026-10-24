<script lang="ts">
    import { onMount } from "svelte";
    import { connection, ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
    import { game, MemberType } from "../lib/state.svelte";
    import SmallNeonButton from "../components/SmallNeonButton.svelte";
    import { OpenDoorOutline, ServerOutline, UserOutline } from "flowbite-svelte-icons";
    import type { R3ResultsPhaseData, ResultsPhaseData } from "../lib/phase";
    import { fly } from "svelte/transition";

    let _answers = $derived(((game.phase ? game.phase : {}) as R3ResultsPhaseData).answers || []);
    let answers = $derived.by(() => {
		const phase = (game.phase ? game.phase : {}) as R3ResultsPhaseData;
		const base = _answers.map((a, idx) => {
			return {
				value: a,
				human: phase.wasHuman[idx],
				points: phase.wasHuman[idx] ? phase.humanPoints[idx] : 0,
				unanimous: phase.unanimity[idx],
				numVotes: phase.numVotes[idx],
				creator: phase.wasHuman[idx] ? phase.creators[idx] : "AI"
			};
		});
		return base.map((_, i) => base[game.r3AnswerIndexMapping[i]]);
	});

    onMount(async() => {
		await ejectIfNoConnection();
		let { data }: { data: R3ResultsPhaseData, timeRemaining: number, finished: boolean } = await connection().invoke("RefreshR3ResultsPhaseData");
		game.phase = data;
		if(!game.r3AnswerIndexMapping) {
			game.r3AnswerIndexMapping = [];
			for(let i = 0; i < game.phase.answers.length; i++) game.r3AnswerIndexMapping.push(i);
			game.r3AnswerIndexMapping = game.r3AnswerIndexMapping.map(v => ({ value: v, sort: Math.random() })).sort((a, b) => a.sort - b.sort).map(v => v.value);
		}
	});

	let ready = $state(false);
	onMount(() => ready = true);

	$inspect(answers);
</script>

{#if ready}
<main in:fly={{ delay: 500, duration: 500, x: 0, y: 100 }} out:fly={{ duration: 500, x: 0, y: -100 }}  class="w-full min-h-full flex flex-col items-center px-4">
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
	<div class="w-full min-h-[85dvh] flex flex-col place-content-center px-4">
		<!-- Header -->
		<div class="w-full flex flex-row items-center mb-25">
			<!-- Round indicator -->
			<span class="px-4 py-1 rounded-2xl w-1/5 font-bold text-lg flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
				Round {game.state.round}
			</span>
		</div>

		<!-- Prompt -->
		<p class="text-white/50 text-base text-left mb-2">THE PROMPT WAS:</p>
		<p class="text-white font-black text-4xl text-left mb-4 pb-6">{((game.phase ? game.phase : {}) as R3ResultsPhaseData).prompt || "An amazing prompt"}</p>

		<!-- Responses -->
		<div class="grid gap-5 grid-cols-1 grid-rows-[{game.r3AnswerIndexMapping.length}]">
			{#each answers as ans}
				<div class="px-8 py-4 rounded-2xl font-normal text-lg bg-white/5 border-2 border-white/20 text-white flex flex-col mb-8 justify-start items-center w-full">
					{#if ans.human}
						<div class="flex flex-row justify-start items-center w-full mb-4 text-white/50 text-sm">
							<span class="bg-[#FF2D78] text-white font-bold text-sm px-3 py-1 rounded-full flex flex-row items-center"><UserOutline size="md" />&nbsp;HUMAN</span>
							&nbsp;&nbsp;&nbsp;
							{(() => {
								if(ans.creator === "AI" || !ans.creator) return "Someone";
								else return game.state.members.find((m) => m.uuid === ans.creator)!.name;
							})()}
						</div>
					{:else}
						<div class="flex flex-row justify-start items-center w-full mb-4">
							<span class="bg-[#00E5FF] text-[#1A1035] font-bold text-sm px-8 py-1 rounded-full flex flex-row items-center"><ServerOutline size="md" />&nbsp;AI</span>
						</div>
					{/if}
					&nbsp;&nbsp;&nbsp;
					<span class="w-full text-left font-normal wrap-break-word">{ans.value}</span>
					{#if ans.human}
						<div class="flex flex-row justify-start items-center w-full">
							<div class="w-11/12 rounded-full h-2 bg-white/10">
								<div class="rounded-full h-2 bg-[#FF2D78]" style={`width: ${Math.max(0, Math.min(ans.points, 1000)) / 10}%`}></div>
							</div>
							&nbsp;&nbsp;&nbsp;
							<span class="text-[#FF2D78] text-4xl font-bold">+{ans.points}</span>
						</div>
						<p class="text-sm text-white/50 text-left w-full">{ans.numVotes} votes</p>
						{#if ans.unanimous}
							<span class="text-[#FFE600] text-base font-bold text-left w-full">Unanimity bonus!</span>
						{/if}
					{:else if ans.unanimous}
						<p class="text-sm text-white/50 text-left w-full">{ans.numVotes} votes</p>
						<span class="text-[#FFE600] text-base font-bold text-left w-full">Unanimity — seems you got out-funnied!</span>
					{/if}
				</div>
			{/each}
		</div>
	</div>	
</main>
{/if}