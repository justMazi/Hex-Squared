<script lang="ts">
	import { onMount } from 'svelte';
	import { Card, CardContent, CardHeader, CardTitle, CardFooter } from '$lib/components/ui/card';
	import { Copy, Dice6 } from 'lucide-svelte';
	import { Button } from '$lib/components/ui/button';
	import { Github } from 'lucide-svelte';
	import toast from 'svelte-french-toast';
	import hex2Logo from '$lib/assets/images/honeycomb.svg';
	const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

	let gameCode = generateGameCode();
	let link: string | null = null;
	let aiTypes = [];
	let selectedAI = null;
	onMount(async () => {
		if (typeof window !== 'undefined') {
			link = `${window.location.origin}/${gameCode}`;
		}

		const res = await fetch(`${API_BASE_URL}/api/v1/AI`);
		aiTypes = await res.json();
		selectedAI = aiTypes[0];
	});

	const copyToClipboard = () => {
		if (link) {
			navigator.clipboard.writeText(link);
			toast.success('Link copied to clipboard');
		}
	};

	function generateGameCode() {
		return Math.random().toString(36).substring(2, 8);
	}

	function regenerateGameCode() {
		gameCode = generateGameCode();
		if (typeof window !== 'undefined') {
			link = `${window.location.origin}/${gameCode}`;
			toast.success('Game code regenerated');
		}
	}

	let selectedSize = 6;
</script>

<div class="flex min-h-screen flex-col items-center justify-center bg-gray-100">
	<!-- Centered Image -->
	<img src={hex2Logo} alt="hex2" class="h-16 w-16" />

	<!-- Centered Title -->
	<h1 class="mt-8 text-center text-4xl font-bold text-gray-800">Hex<sup>2</sup></h1>

	<!-- Card Component -->
	<Card class="mt-8 w-full max-w-md rounded-lg bg-white p-6 shadow-md">
		<CardHeader>
			<CardTitle class="text-lg font-semibold text-gray-800">Start Game</CardTitle>
		</CardHeader>
		<CardContent>
			<div class="flex flex-col space-y-4 text-sm text-gray-700">
				<p>Send the link to your friends so they can join the game or simply just spectate.</p>
				<div class="flex items-center space-x-2">
					<input
						id="link"
						type="text"
						value={link}
						disabled
						class="flex-grow rounded-md border border-gray-300 px-3 py-2 text-gray-700"
					/>
					<Button
						size="sm"
						on:click={copyToClipboard}
						aria-label="Copy Link"
						class="bg-gray-800 text-white hover:bg-gray-700"
					>
						<Copy class="h-4 w-4" /> Copy
					</Button>
					<Button
						size="sm"
						on:click={regenerateGameCode}
						aria-label="Regenerate Game Code"
						class="bg-gray-800 text-white hover:bg-gray-700"
					>
						<Dice6 class="h-4 w-4" />
					</Button>
				</div>
			</div>

			<h1 class="pt-4 text-sm font-semibold text-gray-800">Select board size</h1>
			<div class="mx-auto flex items-center justify-center space-x-4 pt-2">
				{#each [4, 6, 10] as size}
					<img
						src={hex2Logo}
						alt="hex2"
						class={`max-h-20 duration-300 ${selectedSize === size ? 'grayscale-0' : 'grayscale'}`}
						style="height: {size * 6}px"
						on:click={() => (selectedSize = size)}
					/>
				{/each}
			</div>

			<div class="mx-auto w-full max-w-sm">
				<label class="mb-2 block pt-4 text-sm font-semibold text-gray-800">
					Select AI to use in this game
				</label>
				<div class="relative">
					<select
						class="block w-full rounded border border-gray-300 bg-white px-3 py-2 text-gray-800 shadow-sm focus:border-blue-500 focus:outline-none focus:ring focus:ring-blue-200"
						bind:value={selectedAI}
					>
						{#each aiTypes as type}
							<option value={type}>{type}</option>
						{/each}
					</select>
				</div>
			</div>
		</CardContent>
		<CardFooter class="mt-4 flex space-x-2">
			<Button
				variant="secondary"
				class="w-full bg-gray-200 text-gray-800 hover:bg-gray-300"
				on:click={() =>
					(window.location.href = `${link}?size=${selectedSize || ''}&aiType=${encodeURIComponent(
						selectedAI || ''
					)}`)}
			>
				Enter the game
			</Button>
		</CardFooter>
	</Card>

	<!-- GitHub Logo with Link -->
	<a
		href="https://github.com/justMazi/Hex-Squared"
		target="_blank"
		rel="noopener noreferrer"
		class="mt-8 flex items-center text-gray-600 hover:text-gray-900"
	>
		<Github alt="GitHub" class="mr-2 h-6 w-6" />
		<span>View on GitHub</span>
	</a>
</div>
