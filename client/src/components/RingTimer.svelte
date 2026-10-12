<script lang="ts">
    import { onMount } from "svelte";

	interface Props {
		duration: number,
		size: number,
		stroke: number,
		class?: string;
	}

	let props: Props = $props();

	let timeRemaining = $state(0);
	$effect(() => {
		const interval = setInterval(() => {
			timeRemaining -= 1000
		}, 1000);
		setTimeout(() => clearInterval(interval), props.duration);

		return () => clearInterval(interval);
	});

	onMount(() => {
		timeRemaining = props.duration;
	});

	let radius: number = $derived((props.size - props.stroke) / 2);
	let circumference: number = $derived(radius * Math.PI * 2);
	let secondsRemaining: number = $derived(timeRemaining / 1000);
	let urgent: boolean = $derived(secondsRemaining < 10);
	let progress: number = $derived(Math.min(1, Math.max(0, timeRemaining / props.duration)));
</script>

<div class="relative flex items-center justify-center w-[{props.size}] h=[{props.size}]">
	<svg width={props.size} height={props.size} class="-rotate-90">
        <circle
          cx={props.size / 2}
          cy={props.size / 2}
          r={radius}
          fill="none"
          stroke="rgba(255,255,255,0.08)"
          stroke-width={props.stroke}
        />
        <circle
          cx={props.size / 2}
          cy={props.size / 2}
          r={radius}
          fill="none"
          stroke={urgent ? '#FF2D78' : '#00E5FF'}
          stroke-width={props.stroke}
          stroke-linecap="round"
          stroke-dasharray={circumference}
          stroke-dashoffset={circumference * (1 - progress)}
          class="transition-all duration-100 filter-[drop-shadow(0 0 4px ${urgent ? '#FF2D78' : '#00E5FF'})]"
          style=""
        />
      </svg>
      <div class="absolute flex flex-col items-center">
        <span class={`text-xl font-black ${urgent ? 'text-[#FF2D78]' : 'text-white'}`}>
          {secondsRemaining}
        </span>
      </div>
</div>