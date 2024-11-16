<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import HexTile from './HexTile.svelte';
	import { page } from '$app/stores';
	import { type SessionCookieData } from './SessionCookieData';

	const hexSize = 30;

	$: currentPlayer = 0;

	let hexGrid = [];
	let gameId;
	let game;

	export let sessionData: SessionCookieData | undefined = undefined;
	currentPlayer = sessionData ? sessionData.PlayerNumber : 0;

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

	// Change player color
	function selectColor(color: number) {
		currentPlayer = color;
		console.log(`Player color changed to: ${color}`);
	}

	function fillWithAI() {
		console.log('Filling remaining players with AI...');
		fetch(`http://localhost:5059/api/v1/game/${gameId}/fill-ai`, { method: 'POST' })
			.then((response) => response.json())
			.then((data) => {
				console.log('AI players added:', data);
				fetchGameData(gameId); // Refresh game data
			})
			.catch((error) => console.error('Error filling AI players:', error));
	}
</script>

<div class="relative flex h-screen w-screen items-center justify-center">
	<svg
		class="absolute inset-0 h-full max-h-full w-full max-w-full"
		viewBox="-750 -750 1500 1500"
		preserveAspectRatio="xMidYMid meet"
	>
		{#each hexGrid as { q, r, s, owner }}
			<HexTile {q} {r} {s} {hexSize} {owner} browserPlayer={currentPlayer} />
		{/each}
	</svg>

	<div class="absolute left-4 top-4 flex gap-2">
		<!-- Buttons for selecting player color -->
		<button
			class="rounded bg-red-500 px-4 py-2 font-bold text-white hover:bg-red-700"
			on:click={() => selectColor(1)}
		>
			Red
		</button>
		<button
			class="rounded bg-green-500 px-4 py-2 font-bold text-white hover:bg-green-700"
			on:click={() => selectColor(2)}
		>
			Green
		</button>
		<button
			class="rounded bg-blue-500 px-4 py-2 font-bold text-white hover:bg-blue-700"
			on:click={() => selectColor(3)}
		>
			Blue
		</button>

		<!-- Button to fill remaining players with AI -->
		<button
			class="rounded bg-gray-500 px-4 py-2 font-bold text-white hover:bg-gray-700"
			on:click={fillWithAI}
		>
			Fill with AI
		</button>
	</div>
</div>
