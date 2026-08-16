<script lang="ts">
	interface Props {
		variant: 'Red' | 'Cyan' | 'Yellow' | 'Translucent';
		onclick: () => void;
		disabled?: boolean;
		children: () => any;
		class?: string;
	}

	let props: Props = $props();

	const getStyle = () => {
		switch (props.variant) {
			case 'Red':
				return 'bg-[#FF2D78] text-white shadow-[0_0_20px_rgba(255,45,120,0.4)] hover:shadow-[0_0_35px_rgba(255,45,120,0.6)]';
			case 'Cyan':
				return 'bg-[#00E5FF] text-[#1A1035] shadow-[0_0_20px_rgba(0,229,255,0.4)] hover:shadow-[0_0_35px_rgba(0,229,255,0.6)]';
			case 'Yellow':
				return 'bg-[#FFE600] text-[#1A1035] shadow-[0_0_20px_rgba(255,230,0,0.4)] hover:shadow-[0_0_35px_rgba(255,230,0,0.6)]';
			case 'Translucent':
				return 'bg-white/5 border-2 border-white/20 text-white hover:border-white/40 hover:bg-white/10';
		}
	};
</script>

<button class={'px-8 py-4 rounded-2xl font-bold text-lg transition-all duration-300 disabled:opacity-30 disabled:cursor-not-allowed active:scale-95 flex items-center justify-center ' + getStyle() + ' ' + (props.class || '')} onclick={props.onclick} disabled={props.disabled}>
	{@render props.children?.()}
</button>
