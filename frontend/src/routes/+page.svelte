<script>
	import HexTile from './HexTile.svelte';

	const radius = 11;
	const hexSize = 30;
	let currentPlayer = 'none'; // This can be bound to a cookie or app state

	// Generate axial coordinates for the grid
	function generateHexGrid(radius) {
		let hexes = [];
		for (let q = -radius; q <= radius; q++) {
			const r1 = Math.max(-radius, -q - radius);
			const r2 = Math.min(radius, -q + radius);
			for (let r = r1; r <= r2; r++) {
				const s = -q - r;
				const isEdge = Math.abs(q) === radius || Math.abs(r) === radius || Math.abs(s) === radius;
				hexes.push({ q, r, s, isEdge });
			}
		}
		return hexes;
	}

	let hexGrid = generateHexGrid(radius);
</script>

<div class="h-[100vh] w-[100vw]">
	<!-- The SVG will now fill the div and resize accordingly -->
	<svg
		class="svg-hex responsive-svg"
		viewBox="-750 -750 1500 1500"
		preserveAspectRatio="xMidYMid meet"
	>
		{#each hexGrid as { q, r, s, isEdge }}
			<HexTile {q} {r} {s} {hexSize} {isEdge} {currentPlayer} />
		{/each}
	</svg>
</div>

<style>
	.hex-container {
		/* Makes the container fill its parent */
		width: 100%;
		height: 100%;
		display: flex;
		justify-content: center;
		align-items: center;
		position: relative;
	}

	.svg-hex {
		/* Ensures the SVG will scale inside the container */
		width: 100%;
		height: 100%;
		max-width: 100%;
		max-height: 100%;
	}

	.responsive-svg {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
	}
</style>
