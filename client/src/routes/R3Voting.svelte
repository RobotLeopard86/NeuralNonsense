<script lang="ts">
	import { onMount } from "svelte";
	import { connection, ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
	import { game, MemberType } from "../lib/state.svelte";
	import SmallNeonButton from "../components/SmallNeonButton.svelte";
	import { CheckCircleOutline, CheckCircleSolid, OpenDoorOutline } from "flowbite-svelte-icons";
	import type { R3VotePhaseData } from "../lib/phase";
	import RingTimer from "../components/RingTimer.svelte";
	import NeonButton from "../components/NeonButton.svelte";
	import LoadingSpinner from "../components/LoadingSpinner.svelte";
	import { fly } from "svelte/transition";

	let sending: boolean[] = $state([]);
	let selected: boolean[] = $state([]);
	let timeUp = $state(false);
	let _answers = $derived(((game.phase ? game.phase : {}) as R3VotePhaseData).answers || []);
	let answers = $derived(_answers.map((_, i) => ({ value: _answers[game.r3AnswerIndexMapping[i]], originalIdx: game.r3AnswerIndexMapping[i]})));

	const toggleAnswer = async(index: number) => {
		if(!selected[index] && selected.filter(v => v).length + sending.filter(v => v).length >= ((game.phase ? game.phase : {}) as R3VotePhaseData).selectCount) return;
		sending[index] = true;
		await connection().invoke(selected[index] ? "RmVoteR3" : "AddVoteR3", index);
		selected[index] = !selected[index];
		sending[index] = false;
	};

	let initialTimeRemaining: number = $derived(((game.phase ? game.phase : {}) as R3VotePhaseData).timer || 1200000);
	onMount(async() => {
		await ejectIfNoConnection();
		let { data, timeRemaining }: { data: R3VotePhaseData, timeRemaining: number, finished: boolean } = await connection().invoke("RefreshR3VotePhaseData");
		game.phase = data;
		initialTimeRemaining = timeRemaining;
		timeUp = initialTimeRemaining <= 0;
		selected = await connection().invoke("RefreshR3Votes");
		game.r3AnswerIndexMapping = [];
		for(let i = 0; i < game.phase.answers.length; i++) {
			game.r3AnswerIndexMapping.push(i);
			sending.push(false);
		}
		game.r3AnswerIndexMapping = game.r3AnswerIndexMapping.map(v => ({ value: v, sort: Math.random() })).sort((a, b) => a.sort - b.sort).map(v => v.value);
	});

	let ready = $state(false);
	onMount(() => ready = true);
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
	{#if game.self.type === MemberType.Player || game.self.type === MemberType.Audience}
		<div class="w-full min-h-[85dvh] flex flex-col place-content-center px-4">
			{#if !timeUp}
				<div out:fly={{ duration: 500, x: 0, y: 100 }}>
					<!-- Header -->
					<div class="w-full flex flex-row items-center mb-25">
						<!-- Round indicator -->
						<span class="px-4 py-1 rounded-2xl w-1/3 font-bold text-lg flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
							Round {game.state.round}
						</span>

						<!-- Spacer -->
						<span class="mx-auto"></span>
									
						<!-- Timer -->
						<RingTimer size={72} stroke={4} duration={game.phase ? (game.phase as R3VotePhaseData).timer : 1200000} {initialTimeRemaining} onExpire={() => timeUp = true} />
					</div>

					<!-- Prompt -->
					<p class="text-white/50 text-base text-left mb-2">THE PROMPT WAS:</p>
					<p class="text-white font-black text-4xl text-left mb-4 pb-6">{((game.phase ? game.phase : {}) as R3VotePhaseData).prompt || "An amazing prompt"}</p>

					<!-- Responses -->
					<p class="text-white/50 text-sm text-left mb-2">Selected {selected.filter(v => v).length}/{((game.phase ? game.phase : {}) as R3VotePhaseData).selectCount} answers</p>
					<p class="text-white font-bold text-sm text-left mb-2">Scroll down to be sure you see everything!</p>
					<div class="grid gap-5 grid-cols-1 grid-rows-[{game.r3AnswerIndexMapping.length}]">
						{#each answers.filter(a => a.originalIdx !== (((game.phase ? game.phase : {}) as R3VotePhaseData).yourAnswerIndex || -1)) as ans}
							<NeonButton variant="Translucent" onclick={() => toggleAnswer(ans.originalIdx)} disabled={sending[ans.originalIdx]} class="flex flex-row mb-8 justify-start items-center w-full">
								{#if sending[ans.originalIdx]}
									<LoadingSpinner size={30} stroke={4} minDegrees={1} maxDegrees={120} />
								{:else}
									{#if selected[ans.originalIdx]}
										<CheckCircleSolid size="lg" class="text-[#00E5FF]" />
									{:else}
										<CheckCircleOutline size="lg" class="text-white" />
									{/if}
								{/if}
								&nbsp;&nbsp;&nbsp;
								<span class="text-left font-normal">{ans.value}</span>
							</NeonButton>
						{/each}
					</div>
				</div>
			{:else}
				<div in:fly={{ delay: 500, duration: 500, x: 0, y: -100 }}>
					<LoadingSpinner size={150} stroke={7} minDegrees={1} maxDegrees={120} />
					<br/>
					<p class="text-white font-bold text-4xl text-center mb-4">Time's up!</p>
					<p class="text-white/50 text-lg text-center">Results are on their way...</p>
				</div>
			{/if}
		</div>
	{:else}
		<div class="w-full min-h-[85dvh] flex flex-col place-content-center px-4">
			<!-- Header -->
			<div class="w-full flex flex-row items-center mb-25">
				<!-- Round indicator -->
				<span class="px-4 py-1 rounded-2xl w-1/3 font-bold text-lg flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
					Round {game.state.round}
				</span>

				<!-- Spacer -->
				<span class="mx-auto"></span>
							
				<!-- Timer -->
				<RingTimer size={72} stroke={4} duration={game.phase ? (game.phase as R3VotePhaseData).timer : 1200000} {initialTimeRemaining} />
			</div>

			<!-- Prompt -->
			<p class="text-white/50 text-base text-left mb-2">THE PROMPT WAS:</p>
			<p class="text-white font-black text-4xl text-left mb-4 pb-6">{((game.phase ? game.phase : {}) as R3VotePhaseData).prompt || "An amazing prompt"}</p>

			<!-- Responses -->
			<p class="text-white/50 text-sm text-left mb-2">Selected {selected.filter(v => v).length}/{((game.phase ? game.phase : {}) as R3VotePhaseData).selectCount} answers</p>
			<div class="grid gap-5 grid-cols-1 lg:grid-cols-2 grid-rows-[{game.r3AnswerIndexMapping.length}] lg:grid-rows-[{game.r3AnswerIndexMapping.length / 2}]">
				{#each answers.filter(a => a.originalIdx !== (((game.phase ? game.phase : {}) as R3VotePhaseData).yourAnswerIndex || 255)) as ans}
					<div class="px-8 py-4 rounded-2xl font-normal text-lg bg-white/5 border-2 border-white/20 text-white flex flex-row mb-8 justify-start items-center w-full">
						<svg class="w-6 h-6 shrink-0 text-white" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"> 
							<path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.5 11.5 11 14l4-4m6 2a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/>
						</svg>
						&nbsp;&nbsp;&nbsp;
						<span class="font-normal">{ans.value}</span>
					</div>
				{/each}
			</div>

			<!-- Spinner -->
			<div class="flex flex-row items-center">
				<LoadingSpinner size={25} stroke={2} minDegrees={1} maxDegrees={120} />
				<span>&nbsp;&nbsp;&nbsp;&nbsp;</span>
				<p class="text-center text-white/50 text-sm">Waiting for votes...</p>
			</div>
		</div>	
	{/if}
</main>
{/if}