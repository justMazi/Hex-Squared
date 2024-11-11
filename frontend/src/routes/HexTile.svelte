<script lang="ts">
	export let q;
	export let r;
	export let s;
	export let hexSize;
	export let owner;

	let hexColor;
	if (owner === 1) {
		hexColor = 'fill-red-600';
	} else if (owner === 2) {
		hexColor = 'fill-green-600';
	} else if (owner === 3) {
		hexColor = 'fill-blue-600';
	} else if (q === 11 || r === 11 || s === 11) {
		hexColor = 'fill-gray-500';
	} else {
		hexColor = 'fill-gray-300';
	}
</script>

<g
	transform="translate(
      {hexSize * Math.sqrt(3) * (q + r / 2)}, 
      {((hexSize * 3) / 2) * r}
    )"
>
	<polygon
		class="rotate-90 stroke-white stroke-2 {hexColor}"
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
