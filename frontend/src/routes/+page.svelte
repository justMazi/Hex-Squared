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

<div class="flex min-h-screen items-center justify-center bg-gray-100 px-4 py-6 sm:px-6">
	<div class="flex w-full max-w-sm flex-col items-center space-y-6">
		<!-- Header Section -->
		<div class="flex flex-col items-center space-y-3">
			<img src={hex2Logo} alt="hex2" class="h-12 w-12 sm:h-16 sm:w-16" />
			<h1 class="text-center text-3xl font-bold text-gray-800 sm:text-4xl">Hex<sup>2</sup></h1>
		</div>

		<!-- Card Component -->
		<Card class="w-full rounded-lg bg-white p-4 shadow-md sm:p-6">
			<CardHeader class="pb-4">
				<CardTitle class="text-base font-semibold text-gray-800 sm:text-lg">Start Game</CardTitle>
			</CardHeader>
			<CardContent class="space-y-4">
				<p class="text-sm text-gray-700">
					Send the link to your friends so they can join the game or simply just spectate.
				</p>

				<!-- Link Input Section -->
				<div class="space-y-2">
					<input
						id="link"
						type="text"
						value={link}
						disabled
						class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm text-gray-700"
					/>
					<div class="flex space-x-2">
						<Button
							size="sm"
							on:click={copyToClipboard}
							aria-label="Copy Link"
							class="flex-1 bg-gray-800 text-white hover:bg-gray-700"
						>
							<Copy class="mr-1 h-4 w-4" /> Copy
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

				<!-- Board Size Selection -->
				<div class="space-y-2">
					<h2 class="text-sm font-semibold text-gray-800">Select board size</h2>
					<div class="flex items-center justify-center space-x-4">
						{#each [4, 6, 10] as size}
							<button
								class="rounded-lg p-3 transition-all duration-300"
								on:click={() => (selectedSize = size)}
							>
								<img
									src={hex2Logo}
									alt="hex2"
									class={`transition-all duration-300 ${selectedSize === size ? 'grayscale-0' : 'grayscale'}`}
									style="height: {size * 6}px"
								/>
							</button>
						{/each}
					</div>
				</div>

				<!-- AI Selection -->
				<div class="space-y-2">
					<label class="block text-sm font-semibold text-gray-800">
						Select AI to use in this game
					</label>
					<select
						class="block w-full rounded border border-gray-300 bg-white px-3 py-2 text-sm text-gray-800 shadow-sm focus:border-blue-500 focus:outline-none focus:ring focus:ring-blue-200"
						bind:value={selectedAI}
					>
						{#each aiTypes as type}
							<option value={type}>{type}</option>
						{/each}
					</select>
				</div>
			</CardContent>

			<CardFooter class="pt-4">
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

		<!-- GitHub Link -->
		<a
			href="https://github.com/justMazi/Hex-Squared"
			target="_blank"
			rel="noopener noreferrer"
			class="flex items-center text-sm text-gray-600 hover:text-gray-900"
		>
			<Github alt="GitHub" class="mr-2 h-5 w-5" />
			<span>View on GitHub</span>
		</a>
	</div>
</div>
