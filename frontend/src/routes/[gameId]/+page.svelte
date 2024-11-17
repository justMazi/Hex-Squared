<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import HexTile from './HexTile.svelte';
	import { page } from '$app/stores';
	import { type SessionCookieData } from './SessionCookieData';
	import { toast } from 'svelte-french-toast';

	const hexSize = 30;

	let currentPlayer: number | null = null;

	let hexGrid = [];
	let gameId: string | null = null;
	let game: any = null;

	let players = game?.players || [];
	export let sessionData: SessionCookieData;

	// Extract game ID from URL
	$: {
		gameId = $page.url.pathname.slice(1); // Assumes the ID is after the slash
	}

	// Fetch game data at intervals
	onMount(() => {
		const cookieString = document.cookie;
		const cookies = Object.fromEntries(
			cookieString.split('; ').map((c) => {
				const [key, value] = c.split('=');
				return [key, value];
			})
		);

		const hexSession = cookies['hex_session'];
		if (hexSession) {
			try {
				sessionData = JSON.parse(decodeURIComponent(hexSession)) as SessionCookieData;
				currentPlayer = sessionData.PlayerNumber;
				console.log('Session data:', sessionData);
			} catch (error) {
				console.error('Failed to parse hex session cookie', error);
				currentPlayer = null;
			}
		} else {
			console.warn('hex_session cookie not found');
			currentPlayer = null;
		}

		if (gameId) {
			refreshGameData(gameId);
			const interval = setInterval(() => refreshGameData(gameId), 2000);

			// Properly register onDestroy
			onDestroy(() => {
				clearInterval(interval);
			});
		}
	});

	// Fetch game data and update hexGrid
	async function refreshGameData(id: string) {
		try {
			const response = await fetch(`http://localhost:5059/api/v1/game/${id}`);
			if (!response.ok) {
				throw new Error(`Failed to fetch game data: ${response.statusText}`);
			}

			const data = await response.json();
			game = data;
			players = data.players || [];
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
		fetch(`http://localhost:5059/api/v1/game/${gameId}/pickColor?color=${color}`, {
			method: 'POST',
			credentials: 'include'
		})
			.then(async (data) => {
				if (data.ok) {
					currentPlayer = color;
					toast.success('Player color selected!');
				} else {
					toast.error('Cannot select this color!');
				}

				refreshGameData(gameId!);
			})
			.catch((error) => toast.error('Error selecting player color:'));
	}

	function fillWithAI() {
		console.log('Filling remaining players with AI...');
		fetch(`http://localhost:5059/api/v1/game/${gameId}/fill-ai`, { method: 'POST' })
			.then((response) => response.json())
			.then((data) => {
				console.log('AI players added:', data);
				refreshGameData(gameId!);
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
		{#each Array(3) as _, i}
			<button
				class="rounded px-4 py-2 font-bold text-white"
				class:bg-red-500={i === 0 && !(players && players[i] !== null)}
				class:bg-green-500={i === 1 && !(players && players[i] !== null)}
				class:bg-blue-500={i === 2 && !(players && players[i] !== null)}
				class:bg-gray-400={players && players[i] !== null}
				class:cursor-not-allowed={players && players[i] !== null}
				class:hover:bg-gray-400={players && players[i] !== null}
				on:click={() => selectColor(i + 1)}
				disabled={players && players[i] !== null}
			>
				{i === 0 ? 'Red' : i === 1 ? 'Green' : 'Blue'}
			</button>
		{/each}

		<!-- Button to fill remaining players with AI -->
		<button
			class="rounded bg-gray-500 px-4 py-2 font-bold text-white hover:bg-gray-700"
			on:click={fillWithAI}
		>
			Fill with AI
		</button>
	</div>
</div>

<style>
	button:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}
</style>
