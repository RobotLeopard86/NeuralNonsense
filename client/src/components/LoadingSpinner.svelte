<script lang="ts">
	interface Props {
		size: number;
		stroke: number;
		minDegrees: number;
		maxDegrees: number;
		class?: string;
	}

	let props: Props = $props();

	let begin = $state(0);
	let range = $state(0);
	let growRange = $state(true);

	$effect(() => {
		range = props.minDegrees;
		const interval = setInterval(() => {
			begin = (begin + 2) % 360;

			if (growRange) {
				if (range >= props.maxDegrees) {
					growRange = false;
				} else {
					range++;
				}
			} else {
				if (range <= props.minDegrees) {
					growRange = true;
				} else {
					range--;
				}
			}
		}, 1000 / 360);

		return () => clearInterval(interval);
	});

	let radius = $derived((props.size - props.stroke) / 2);
</script>

<div class="relative flex items-center justify-center w-[{props.size}] h=[{props.size}]">
	<svg width={props.size} height={props.size} class="-rotate-90">
		<circle cx={props.size / 2} cy={props.size / 2} r={radius} fill="none" stroke="rgba(255,255,255,0.08)" stroke-width={props.stroke} />
		<circle
			cx={props.size / 2}
			cy={props.size / 2}
			r={radius}
			pathLength="360"
			fill="none"
			stroke="#00E5FF"
			stroke-width={props.stroke}
			stroke-linecap="round"
			stroke-dasharray={`${range} ${360 - range}`}
			stroke-dashoffset={-begin}
			style="filter: drop-shadow(0 0 4px #00E5FF);"
		/>
	</svg>
</div>
