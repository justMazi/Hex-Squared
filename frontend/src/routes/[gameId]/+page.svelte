<script>
	import { onMount, onDestroy } from 'svelte';
	import HexTile from '../HexTile.svelte';
	import { page } from '$app/stores';

	const hexSize = 30;
	let currentPlayer = 'none';
	let hexGrid = [];
	let gameId;
	let game;

	// Extract game ID from URL
	$: {
		gameId = $page.url.pathname.slice(1); // Assumes the ID is after the slash
	}

	// Fetch game data at intervals
	onMount(() => {
		if (gameId) {
			fetchGameData(gameId);
			const interval = setInterval(() => fetchGameData(gameId), 2000);

			// Clear the interval when component is destroyed
			onDestroy(() => clearInterval(interval));
		}
	});

	// Fetch game data and update hexGrid
	async function fetchGameData(id) {
		try {
			const response = await fetch(`http://localhost:5059/api/v1/game/${id}`);
			const data = await response.json();

			game = data;
			currentPlayer = data.currentMovePlayerIndex.value;

			hexGrid = data.hexagons.map(({ q, r, s, owner, isTaken }) => ({
				q,
				r,
				s,
				owner,
				isTaken
			}));
		} catch (error) {
			console.error('Error fetching game data:', error);
		}
	}
</script>

<div class="h-[100vh] w-[100vw]">
	<svg
		class="svg-hex responsive-svg"
		viewBox="-750 -750 1500 1500"
		preserveAspectRatio="xMidYMid meet"
	>
		{#each hexGrid as { q, r, s, owner }}
			<HexTile {q} {r} {s} {hexSize} {owner} />
		{/each}
	</svg>
</div>

<style>
	.hex-container {
		width: 100%;
		height: 100%;
		display: flex;
		justify-content: center;
		align-items: center;
		position: relative;
	}

	.svg-hex {
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
