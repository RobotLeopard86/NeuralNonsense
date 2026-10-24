<script lang="ts">
	interface Props {
		size: number,
		stroke: number,
		minDegrees: number,
		maxDegrees: number
	}

	let props: Props = $props();
	let radius = $derived((props.size - props.stroke) / 2);
</script>

<div class="relative flex items-center justify-center w-[{props.size}] h-[{props.size}]">
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
			class="animate-spinner"
			style="
				filter: drop-shadow(0 0 4px #00E5FF);
				--min-deg: {props.minDegrees};
				--max-deg: {props.maxDegrees};
			" />
	</svg>
</div>

<style>
	.animate-spinner {
		animation: spin 2s linear infinite, grow-shrink 2s ease-in-out infinite;
	}

	@keyframes spin {
		from {
			stroke-dashoffset: 0;
		}
		to {
			stroke-dashoffset: -360;
		}
	}

	@keyframes grow-shrink {
		0%, 100% {
			stroke-dasharray: var(--min-deg) calc(360px - var(--min-deg));
		}
		50% {
			stroke-dasharray: var(--max-deg) calc(360px - var(--max-deg));
		}
	}
</style>
