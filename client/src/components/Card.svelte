<script lang="ts">
	interface Props {
		class?: string;
		children: () => any;
	}

	let props: Props = $props();
</script>

<div class={'px-4 py-4 rounded-2xl text-lg bg-white/5 border-2 border-white/10 ' + (props.class || '')}>
	{@render props.children?.()}
</div>
