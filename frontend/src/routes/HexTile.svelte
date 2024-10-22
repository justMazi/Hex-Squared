<script lang="ts">
	export let q;
	export let r;
	export let s;
	export let hexSize;
	export let isEdge = false;
	export let currentPlayer = 'none';

	let hoverColor = '';

	// Set hover color based on the current player
	function handleMouseEnter() {
		if (currentPlayer === 'player1') {
			hoverColor = 'fill-red-400'; // Player 1 color
		} else if (currentPlayer === 'player2') {
			hoverColor = 'fill-green-400'; // Player 2 color
		} else {
			hoverColor = ''; // No player or reset
		}
	}

	function handleMouseLeave() {
		hoverColor = ''; // Reset hover color
	}

	$: hexColor = isEdge ? 'fill-blue-300' : 'fill-gray-300'; // Edge tiles will have a different color
</script>

<g
	transform="translate(
      {hexSize * Math.sqrt(3) * (q + r / 2)}, 
      {((hexSize * 3) / 2) * r}
    )"
	class="cursor-pointer transition duration-300"
>
	<polygon
		class={`rotate-90 stroke-white stroke-2 transition ${hoverColor || hexColor}`}
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
