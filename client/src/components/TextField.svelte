<script lang="ts">
	interface Props {
		value: string;
		placeholder: string;
		class?: string;
		maxLength?: number;
	}

	let { value = $bindable(), ...props }: Props = $props();
</script>

<input type="text" bind:value maxlength={props.maxLength || 1000} placeholder={props.placeholder} class={'focus:outline-none px-8 py-4 rounded-2xl text-lg bg-white/5 border-2 border-white/10 text-white placeholder:text-white/30 ' + (props.class || '')} />
