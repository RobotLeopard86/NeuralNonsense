<script lang="ts">
    import { onMount } from "svelte";
    import { connection, ejectIfNoConnection, leaveRoom } from "../lib/connection.svelte";
    import LoadingSpinner from "../components/LoadingSpinner.svelte";
    import { game, MemberType } from "../lib/state.svelte";
    import SmallNeonButton from "../components/SmallNeonButton.svelte";
    import { OpenDoorOutline, PaperPlaneOutline } from "flowbite-svelte-icons";
    import RingTimer from "../components/RingTimer.svelte";
    import type { AnswerPhaseData } from "../lib/phase";
    import NeonButton from "../components/NeonButton.svelte";
    import { fly } from "svelte/transition";
	import xss from "xss";
    import { addToast } from "../lib/toast.svelte";

    let maxAnswerLength = $derived(((game.phase ? game.phase : {}) as AnswerPhaseData).maxAnswerLength || 200);
    let answer = $state("");

	let canSubmit = $derived(answer ? true : false);
	let submitted = $state(false);
	let received = $state(false);
	let timeUp = $state(false);

	const submit = async() => {
		//Mark submitted
		submitted = true;
		
		//Send to server
		try {
			await connection().invoke("SubmitAnswer", xss(answer));
		} catch(e) {
			addToast(`Failed to submit your answer: ${e}`, 5000);
		}

		//Mark received
		received = true;	
		connection().off("RequestMissingAnswer");
	}

	let initialTimeRemaining: number = $derived(((game.phase ? game.phase : {}) as AnswerPhaseData).timer || 60000);
	onMount(async() => {
		await ejectIfNoConnection();
		connection().on("RequestMissingAnswer", () => answer ? answer : "(didn't submit in time!)");
		let { data, timeRemaining, finished }: { data: AnswerPhaseData, timeRemaining: number, finished: boolean } = await connection().invoke("RefreshAnswerPhaseData");
		game.phase = data;
		initialTimeRemaining = timeRemaining;
		received = finished;
		timeUp = initialTimeRemaining <= 0;
	});
	onMount(() => {
		return () => connection().off("RequestMissingAnswer");
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

    {#if game.self.type === MemberType.Player}
		<!-- Score indicator -->
		<div class="absolute top-4 right-4 px-4 py-1 rounded-2xl font-bold text-sm flex items-center justify-center bg-white/5 border-2 border-white/20 text-white">
			{game.self.score} Points
		</div>

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
						<RingTimer size={72} stroke={4} duration={game.phase ? (game.phase as AnswerPhaseData).timer : 60000} {initialTimeRemaining} onExpire={() => timeUp = true} />
					</div>

					<!-- Prompt -->
					<p class="text-white/50 text-base text-left mb-2">YOUR PROMPT:</p>
					<p class="text-white font-black text-4xl text-left mb-4 pb-6">{((game.phase ? game.phase : {}) as AnswerPhaseData).prompt || "An amazing prompt"}</p>

					<!-- Response area -->
					<p class="text-white/50 text-base text-left mb-2">YOUR RESPONSE:</p>
					<textarea name="Answer" placeholder="Respond to the prompt!" rows={3} maxlength={maxAnswerLength} bind:value={answer} disabled={submitted}
						class="w-full bg-white/5 rounded-2xl p-4 text-white text-lg border-2 border-white/10 focus:border-[#FF2D78] focus:outline-none resize-none transition-colors">
					</textarea>
					<p class={`w-full text-right ${answer.length == maxAnswerLength ? "font-semibold text-[#FF2D78]" : "text-white/50"} text-sm mr-8`}>{answer.length}/{maxAnswerLength}</p>

					<!-- Submission button -->
					<NeonButton variant="Yellow" onclick={submit} disabled={!canSubmit || submitted} class="w-full my-4">
						{#if submitted}
							<LoadingSpinner size={25} stroke={2} minDegrees={1} maxDegrees={120} />
							<span>&nbsp;&nbsp;&nbsp;&nbsp;</span>
							<p class="text-center text-lg">Working...</p>
						{:else}
							<PaperPlaneOutline size="xl" /> Submit Answer
						{/if}
					</NeonButton>
				</div>
			{:else if received}
				<div in:fly={{ delay: 500, duration: 500, x: 0, y: -100 }}>
					<LoadingSpinner size={150} stroke={7} minDegrees={1} maxDegrees={120} />
					<br/>
					<p class="text-white font-bold text-4xl text-center mb-4">Response received!</p>
					<p class="text-white/50 text-lg text-center">Waiting for other players...</p>
				</div>
			{:else}
				<div in:fly={{ delay: 500, duration: 500, x: 0, y: -100 }}>
					<LoadingSpinner size={150} stroke={7} minDegrees={1} maxDegrees={120} />
					<br/>
					<p class="text-white font-bold text-4xl text-center mb-4">Time's up!</p>
					<p class="text-white/50 text-lg text-center">Voting will begin soon...</p>
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
				<RingTimer size={72} stroke={4} duration={game.phase ? (game.phase as AnswerPhaseData).timer : 60000} {initialTimeRemaining} />
			</div>

			<!-- Status -->
			<LoadingSpinner size={150} stroke={7} minDegrees={1} maxDegrees={120} />
			<br/>
			<p class="text-white font-bold text-4xl text-center mb-4">Players are thinking...</p>
			{#if game.self.type === MemberType.Audience}
				<p class="text-white/50 text-lg text-center">You'll get to vote in a moment!</p>
			{:else}
				<p class="text-white/50 text-lg text-center">Hang tight for a moment!</p>
			{/if}
		</div>
    {/if}
</main>
{/if}