<script lang="ts">
	import { onMount, onDestroy } from 'svelte';
	import HexTile from './HexTile.svelte';
	import { page } from '$app/stores';
	import { type SessionCookieData } from './SessionCookieData';
	import { toast } from 'svelte-french-toast';
	import { Button } from '$lib/components/ui/button/index.js';
	import * as Dialog from '$lib/components/ui/dialog/index.js';
	import { Input } from '$lib/components/ui/input/index.js';
	import { Label } from '$lib/components/ui/label/index.js';
	import Client from '../../Client/Client';
	import type { Game } from '../../Client/GeneratedClient';
	import client from '../../Client/Client';
	import { goto } from '$app/navigation';

	const hexSize = 30;
	$: radius = 6;
	let isGameOver = false;
	let currentPlayer: number | null = null;
	let score = [3];
	let hexGrid = [];
	let gameId: string;
	let game: Game;
	let gameState;
	let players;
	let selectedAI: string;

	const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

	let isSessionMatch: boolean;

	$: {
		const currentPath = decodeURIComponent($page.url.pathname.slice(1).trim());
		const sessionId = sessionData?.Id ? decodeURIComponent(sessionData.Id.trim()) : null;

		isSessionMatch = sessionId && currentPath && sessionId === currentPath;
	}

	const colors = ['red', 'green', 'blue'];

	export let sessionData: SessionCookieData;

	// Extract game ID from URL
	$: {
		gameId = $page.url.pathname.slice(1); // Assumes the ID is after the slash
	}

	onMount(() => {
		console.log(isNullOrEmpty({}));
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
			const interval = setInterval(() => refreshGameData(gameId), 500);

			onDestroy(() => {
				clearInterval(interval);
			});
		}
	});

	// Fetch game data and update hexGrid
	async function refreshGameData(id: string) {
		try {
			const url = new URL($page.url.href); // Get current page URL
			const size = url.searchParams.get('size'); // Extract 'size' query parameter
			const ai = url.searchParams.get('aiType'); // Extract 'ai' query parameter

			game = await Client.game(id, size, ai); // Pass both size and AI type
			if (game.winner) {
				isGameOver = true;
			}

			players = game.players;
			gameState = game.gameState;
			score = game.players.map((p) => p.numberOfWins);
			radius = game.radius + 1;
			selectedAI = game.aiType;
			hexGrid = game.hexagons.map(({ q, r, s, owner, isTaken }) => ({
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

	function selectColor(color: number) {
		fetch(`${API_BASE_URL}/api/v1/game/${gameId}/pickColor?color=${color}`, {
			method: 'POST',
			credentials: 'include'
		})
			.then(async (data) => {
				if (data.ok) {
					currentPlayer = color;
					toast.success('Player color selected!');
				} else if (data.redirected) {
					window.location.href = new URL(data.url).pathname;
					return;
				} else {
					toast.error('Error selecting player color');
				}

				// Perform full page reload
				window.location.reload();
			})
			.catch((error) => toast.error('Error selecting player color:'));
	}

	function concede() {
		fetch(`${API_BASE_URL}/api/v1/game/${gameId}/concede`, {
			method: 'POST',
			credentials: 'include'
		})
			.then(async (data) => {
				toast.success('You gave up! :(');
			})
			.catch((error) => toast.error('Error selecting player color:'));
	}

	function move(q: number, r: number, s: number) {
		try {
			fetch(`${API_BASE_URL}/api/v1/game/${gameId}/move?q=${q}&r=${r}&s=${s}`, {
				method: 'POST',
				credentials: 'include'
			}).then(async (data) => {
				if (data.ok) {
					toast.success('Move successful!');
				} else {
					toast.error('Cannot move here!');
				}
			});
		} catch (error) {
			console.error('Error moving:', error);
		}
		refreshGameData(gameId!);
	}

	async function fillWithAI() {
		console.log('Filling remaining players with AI...');
		fetch(`${API_BASE_URL}/api/v1/game/${gameId}/fill-with-ai`, { method: 'POST' })
			.then((response) => response.json())
			.then((data) => {
				toast.success('AI players added:', data);
				refreshGameData(gameId!);
			})
			.catch((error) => console.error('Error filling AI players:', error));
		return;
	}

	function isNullOrEmpty(objectName: {} | null) {
		return objectName == null || isEmpty(objectName);
	}
	function isEmpty(objectName: {}) {
		return Object.keys(objectName).length === 0;
	}
</script>

<Dialog.Root open={isGameOver}>
	<Dialog.Content class="sm:max-w-[425px]">
		<Dialog.Header>
			<Dialog.Title>Game Over!</Dialog.Title>
			<Dialog.Description>
				{#if game.winner == -1}
					The game has ended in a draw.
				{:else if game.winner}
					The game has ended. The winner is the {colors[game.winner - 1]} player.
				{/if}
			</Dialog.Description>
		</Dialog.Header>
		<Dialog.Footer>
			<Button
				on:click={() => {
					client.reset(gameId);
					isGameOver = false;
				}}>Play Again</Button
			>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>

<div class="relative flex h-screen w-screen items-center justify-center">
	<svg
		class="absolute inset-0 h-full max-h-full w-full max-w-full"
		viewBox="-750 -750 1500 1500"
		preserveAspectRatio="xMidYMid meet"
	>
		{#each hexGrid as { q, r, s, owner }}
			<HexTile
				{q}
				{r}
				{s}
				{hexSize}
				{owner}
				{isSessionMatch}
				{isGameOver}
				browserPlayer={currentPlayer}
				{radius}
				onClick={() => move(q, r, s)}
			/>
		{/each}
	</svg>

	<div class="absolute left-4 top-4 flex gap-2">
		{#if players && players.every((p) => p !== null)}
			{#each Array(3) as _, i}
				<button
					class="h-12 rounded px-4 py-2 font-bold text-white"
					class:bg-red-500={i === 0 && !(players && isNullOrEmpty(players[i]))}
					class:bg-green-500={i === 1 && !(players && isNullOrEmpty(players[i]))}
					class:bg-blue-500={i === 2 && !(players && isNullOrEmpty(players[i]))}
					class:bg-gray-400={players && isNullOrEmpty(players[i])}
					class:cursor-not-allowed={players && isNullOrEmpty(players[i])}
					class:hover:bg-gray-400={players && isNullOrEmpty(players[i])}
					on:click={() => selectColor(i + 1)}
					disabled={players[i]?.playerNum}
				>
					{i === 0 ? 'Red' : i === 1 ? 'Green' : 'Blue'}
				</button>
			{/each}

			<!-- Button to fill remaining players with AI -->
			<button
				class="h-12 rounded bg-gray-500 px-4 py-2 font-bold text-white hover:bg-gray-700 disabled:hover:bg-gray-500"
				on:click={fillWithAI}
				disabled={players?.every((p) => p?.playerNum)}
			>
				Fill with AI ({selectedAI.split('.').pop()})
			</button>
		{/if}

		{#if isGameOver}
			<Button
				class="h-12 font-semibold"
				on:click={() => {
					client.reset(gameId);
					refreshGameData(gameId);
					isGameOver = false;
				}}>Play Again</Button
			>
		{/if}

		{#if isSessionMatch && !isGameOver && gameState == 1}
			<Button
				class="h-12 bg-red-700 font-semibold hover:bg-red-600"
				on:click={() => {
					concede();
				}}>Concede</Button
			>
		{/if}
	</div>

	<div class="absolute left-4 top-20 flex flex-col gap-1">
		{#if score && score.every((s) => s !== undefined)}
			<h1 class="font-semibold">Scores</h1>
			{#each Array(3) as _, j}
				<div class="flex items-center gap-1">
					<div
						class="h-2 w-2 rounded-full"
						class:bg-red-500={j === 0}
						class:bg-green-500={j === 1}
						class:bg-blue-500={j === 2}
					></div>
					<p class="text-center">{score[j]}</p>
				</div>
			{/each}
		{/if}
	</div>
</div>

<style>
	button:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}
</style>
