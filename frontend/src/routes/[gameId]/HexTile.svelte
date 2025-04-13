<script lang="ts">
	import type { SessionCookieData } from './SessionCookieData';
	export let isGameOver;
	$: gameOver = false;
	export let q;
	export let r;
	export let s;
	export let hexSize;
	export let owner;
	export let browserPlayer;
	export let onClick: () => void | undefined = () => {};
	export let isSessionMatch: boolean;
	export let radius: number;

	let hexColor;
	let hoverColor;
	const fillColor = 'fill-gray-300';

	// Reactive statement to update hoverColor based on browserPlayer
	$: hoverColor =
		browserPlayer === 1
			? 'hover:fill-red-300'
			: browserPlayer === 2
				? 'hover:fill-green-300'
				: browserPlayer === 3
					? 'hover:fill-blue-300'
					: 'hover:';

	// Set initial hex color based on owner
	$: hexColor =
		owner === 1
			? 'fill-red-600'
			: owner === 2
				? 'fill-green-600'
				: owner === 3
					? 'fill-blue-600'
					: q === radius || r === radius || s === radius
						? 'fill-gray-500'
						: fillColor;

	const cookies = Object.fromEntries(document.cookie.split('; ').map((c) => c.split('=')));

	let isHoverable = owner === 0 && ![q, r, s].includes(radius) && isSessionMatch;
</script>

<g
	on:click={gameOver ? undefined : onClick}
	transform="translate(
      {hexSize * Math.sqrt(3) * (q + r / 2)}, 
      {((hexSize * 3) / 2) * r} 
    )"
>
	<polygon
		class="rotate-90 stroke-white stroke-2 {hexColor} {isHoverable ? hoverColor : ''}"
		points=" 
        {hexSize * Math.cos(0)}, {hexSize * Math.sin(0)}
        {hexSize * Math.cos(Math.PI / 3)}, {hexSize * Math.sin(Math.PI / 3)}
        {hexSize * Math.cos((2 * Math.PI) / 3)}, {hexSize * Math.sin((2 * Math.PI) / 3)}
        {hexSize * Math.cos(Math.PI)}, {hexSize * Math.sin(Math.PI)}
        {hexSize * Math.cos((4 * Math.PI) / 3)}, {hexSize * Math.sin((4 * Math.PI) / 3)}
        {hexSize * Math.cos((5 * Math.PI) / 3)}, {hexSize * Math.sin((5 * Math.PI) / 3)}
      "
	/>
</g>
