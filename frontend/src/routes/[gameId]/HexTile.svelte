<script lang="ts">
	export let q;
	export let r;
	export let s;
	export let hexSize;
	export let owner;
	export let browserPlayer;

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
					: q === 11 || r === 11 || s === 11
						? 'fill-gray-500'
						: fillColor;

	let isHoverable = owner === 0 && q != 11 && r != 11 && s != 11;
</script>

<g
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
