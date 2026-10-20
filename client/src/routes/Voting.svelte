<script lang="ts">
    import { onMount } from "svelte";
    import { connection, ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
    import LoadingSpinner from "../components/LoadingSpinner.svelte";
    import { game, MemberType } from "../lib/state.svelte";
    import SmallNeonButton from "../components/SmallNeonButton.svelte";
    import { OpenDoorOutline } from "flowbite-svelte-icons";
    import RingTimer from "../components/RingTimer.svelte";
    import type { VotePhaseData } from "../lib/phase";
    import NeonButton from "../components/NeonButton.svelte";
    import { fly } from "svelte/transition";
    import { addToast } from "../lib/toast.svelte";

	let voted = $state(false);
	let received = $state(false);
	let choseHuman = $state(false);
	let showHumanFirst = $state(false);
	let timeUp = $state(false);

	const submit = async(choseHuman: boolean) => {
		//Mark vote complete
		voted = true;
		choseHuman = choseHuman;
		
		//Send to server
		try {
			await connection().invoke("Vote", choseHuman);
		} catch(e) {
			addToast(`Failed to submit your answer: ${e}`, 5000);
		}

		//Mark received
		received = true;	
	}

	let initialTimeRemaining: number = $derived(((game.phase ? game.phase : {}) as VotePhaseData).timer || 30000);
	onMount(async() => {
		await ejectIfNoConnection();
		let { data, timeRemaining, finished }: { data: VotePhaseData, timeRemaining: number, finished: boolean } = await connection().invoke("RefreshVotePhaseData");
		game.phase = data;
		initialTimeRemaining = timeRemaining;
		received = finished;
		showHumanFirst = Math.random() >= 0.5;
		timeUp = initialTimeRemaining <= 0;
	});
</script>

<main class="w-full min-h-full flex flex-col items-center px-4">
	<!-- Leave button -->
	{#if game.self.type !== MemberType.Projector}
		<SmallNeonButton variant="Red" onclick={leaveRoom} class="absolute top-4 left-4"><OpenDoorOutline size="lg" />&nbsp;&nbsp;Leave Room</SmallNeonButton>
	{/if}

    {#if game.self.type === MemberType.Player || game.self.type === MemberType.Audience}
		<!-- Score indicator -->
		<div class="absolute top-4 right-4 px-4 py-1 rounded-2xl font-bold text-sm flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
			{game.self.score} Points
		</div>
	{/if}

	{#if ((game.phase ? game.phase : {}) as VotePhaseData).humanID !== game.self.uuid && (game.self.type === MemberType.Player || game.self.type === MemberType.Audience)}
        <!-- Body zone-->
		<div class="w-3/4 min-h-[85dvh] flex flex-col place-content-center px-4">
			{#if !received && !timeUp}
				<div out:fly={{ duration: 500, x: 0, y: 100 }}>
					<!-- Header -->
					<div class="w-full flex flex-row items-center mb-8">
						<!-- Round indicator -->
						<span class="px-4 py-1 rounded-2xl w-1/5 font-bold text-lg flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
							Round {game.state.round}
						</span>

						<!-- Spacer -->
						<span class="mx-auto"></span>
						
						<!-- Timer -->
						<RingTimer size={72} stroke={4} duration={game.phase ? (game.phase as VotePhaseData).timer : 30000} {initialTimeRemaining} onExpire={() => timeUp = true} />
					</div>

					<!-- Prompt -->
					<p class="text-white/50 text-base text-left mb-2">THE PROMPT WAS:</p>
					<p class="text-white font-black text-4xl text-left mb-4 pb-6">{((game.phase ? game.phase : {}) as VotePhaseData).prompt || "An amazing prompt"}</p>

					<!-- Voting zone -->
					{#if showHumanFirst}
						<NeonButton variant="Translucent" onclick={() => submit(true)} disabled={voted} class="flex flex-row mb-8 justify-start items-center w-full">
							{#if voted && choseHuman}
								<LoadingSpinner size={30} stroke={4} minDegrees={1} maxDegrees={120} />
							{:else}
								<svg width="40" height="40">
									<circle cx="20" cy="20" r="15" fill="#FF2D78" style="filter: drop-shadow(0 0 2px #FF2D78);" />
									<text x="14" y="26" style="fill: white;">1</text>
								</svg>
							{/if}
							&nbsp;&nbsp;&nbsp;
							<span class="font-normal">{((game.phase ? game.phase : {}) as VotePhaseData).humanAnswer || "Answer A"}</span>
						</NeonButton>
						<NeonButton variant="Translucent" onclick={() => submit(false)} disabled={voted} class="flex flex-row mb-8 justify-start items-center w-full">
							{#if voted && !choseHuman}
								<LoadingSpinner size={30} stroke={4} minDegrees={1} maxDegrees={120} />
							{:else}
								<svg width="40" height="40">
									<circle cx="20" cy="20" r="15" fill="#00E5FF" style="filter: drop-shadow(0 0 2px #00E5FF);" />
									<text x="15" y="26" style="fill: #1A1035;">2</text>
								</svg>
							{/if}
							&nbsp;&nbsp;&nbsp;
							<span class="font-normal">{((game.phase ? game.phase : {}) as VotePhaseData).aiAnswer || "Answer B"}</span>
						</NeonButton>
					{:else}
						<NeonButton variant="Translucent" onclick={() => submit(false)} disabled={voted} class="flex flex-row mb-8 justify-start items-center w-full">
							{#if voted && !choseHuman}
								<LoadingSpinner size={30} stroke={4} minDegrees={1} maxDegrees={120} />
							{:else}
								<svg width="40" height="40">
									<circle cx="20" cy="20" r="15" fill="#FF2D78" style="filter: drop-shadow(0 0 2px #FF2D78);" />
									<text x="14" y="26" style="fill: white;">1</text>
								</svg>
							{/if}
							&nbsp;&nbsp;&nbsp;
							<span class="font-normal">{((game.phase ? game.phase : {}) as VotePhaseData).aiAnswer || "Answer B"}</span>
						</NeonButton>
						<NeonButton variant="Translucent" onclick={() => submit(true)} disabled={voted} class="flex flex-row mb-8 justify-start items-center w-full">
							{#if voted && choseHuman}
								<LoadingSpinner size={30} stroke={4} minDegrees={1} maxDegrees={120} />
							{:else}
								<svg width="40" height="40">
									<circle cx="20" cy="20" r="15" fill="#00E5FF" style="filter: drop-shadow(0 0 2px #00E5FF);" />
									<text x="15" y="26" style="fill: #1A1035;">2</text>
								</svg>
							{/if}
							&nbsp;&nbsp;&nbsp;
							<span class="font-normal">{((game.phase ? game.phase : {}) as VotePhaseData).humanAnswer || "Answer A"}</span>
						</NeonButton>
					{/if}
				</div>
			{:else if received}
				<div in:fly={{ delay: 500, duration: 500, x: 0, y: -100 }}>
					<LoadingSpinner size={150} stroke={7} minDegrees={1} maxDegrees={120} />
					<br/>
					<p class="text-white font-bold text-4xl text-center mb-4">Vote submitted!</p>
					<p class="text-white/50 text-lg text-center">Waiting for everyone else to vote...</p>
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
		<div class="w-3/4 min-h-[85dvh] flex flex-col place-content-center px-4">
			<!-- Header -->
			<div class="w-full flex flex-row items-center mb-25">
				<!-- Round indicator -->
				<span class="px-4 py-1 rounded-2xl w-1/7 font-bold text-lg flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
					Round {game.state.round}
				</span>

				<!-- Spacer -->
				<span class="mx-auto"></span>
				
				<!-- Timer -->
				<RingTimer size={72} stroke={4} duration={game.phase ? (game.phase as VotePhaseData).timer : 30000} {initialTimeRemaining} />
			</div>

			<!-- Prompt -->
			<p class="text-white/50 text-base text-left mb-2">THE PROMPT WAS:</p>
			<p class="text-white font-black text-4xl text-left mb-4 pb-6">{((game.phase ? game.phase : {}) as VotePhaseData).prompt || "An amazing prompt"}</p>

			<!-- Answers -->
			{#if showHumanFirst}
				<div class="px-8 py-4 rounded-2xl font-normal text-lg bg-white/5 border-2 border-white/20 text-white flex flex-row mb-8 justify-start items-center w-full">
					<svg width="40" height="40">
						<circle cx="20" cy="20" r="15" fill="#FF2D78" style="filter: drop-shadow(0 0 2px #FF2D78);" />
						<text x="14" y="26" style="fill: white;">1</text>
					</svg>
					&nbsp;&nbsp;&nbsp;
					{((game.phase ? game.phase : {}) as VotePhaseData).humanAnswer || "Answer A"}
				</div>
				<div class="px-8 py-4 rounded-2xl font-normal text-lg bg-white/5 border-2 border-white/20 text-white flex flex-row mb-8 justify-start items-center w-full">
					<svg width="40" height="40">
						<circle cx="20" cy="20" r="15" fill="#00E5FF" style="filter: drop-shadow(0 0 2px #00E5FF);" />
						<text x="15" y="26" style="fill: #1A1035;">2</text>
					</svg>
					&nbsp;&nbsp;&nbsp;
					{((game.phase ? game.phase : {}) as VotePhaseData).aiAnswer || "Answer B"}
				</div>
			{:else}
				<div class="px-8 py-4 rounded-2xl font-normal text-lg bg-white/5 border-2 border-white/20 text-white flex flex-row mb-8 justify-start items-center w-full">
					<svg width="40" height="40">
						<circle cx="20" cy="20" r="15" fill="#FF2D78" style="filter: drop-shadow(0 0 2px #FF2D78);" />
						<text x="14" y="26" style="fill: white;">1</text>
					</svg>
					&nbsp;&nbsp;&nbsp;
					{((game.phase ? game.phase : {}) as VotePhaseData).aiAnswer || "Answer B"}
				</div>
				<div class="px-8 py-4 rounded-2xl font-normal text-lg bg-white/5 border-2 border-white/20 text-white flex flex-row mb-8 justify-start items-center w-full">
					<svg width="40" height="40">
						<circle cx="20" cy="20" r="15" fill="#00E5FF" style="filter: drop-shadow(0 0 2px #00E5FF);" />
						<text x="15" y="26" style="fill: #1A1035;">2</text>
					</svg>
					&nbsp;&nbsp;&nbsp;
					{((game.phase ? game.phase : {}) as VotePhaseData).humanAnswer || "Answer A"}
				</div>
			{/if}

			<!-- Spinner -->
			<div class="flex flex-row items-center">
				<LoadingSpinner size={25} stroke={2} minDegrees={1} maxDegrees={120} />
				<span>&nbsp;&nbsp;&nbsp;&nbsp;</span>
				<p class="text-center text-white/50 text-sm">Waiting for votes...</p>
			</div>
		</div>
    {/if}
</main>