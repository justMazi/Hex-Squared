<script lang="ts">
	import { onMount } from 'svelte';
	import { Card, CardContent, CardHeader, CardTitle, CardFooter } from '$lib/components/ui/card';
	import { Copy, Dice6 } from 'lucide-svelte';
	import { Button } from '$lib/components/ui/button';
	import { Github } from 'lucide-svelte';
	import toast from 'svelte-french-toast';

	let gameCode = generateGameCode();
	let link: string | null = null;

	onMount(() => {
		if (typeof window !== 'undefined') {
			link = `${window.location.origin}/${gameCode}`;
		}
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
</script>

<div class="flex min-h-screen flex-col items-center justify-center bg-gray-100">
	<!-- Centered Image -->
	<img src="../src/lib/assets/images/honeycomb.svg" alt="hex2" class="h-16 w-16" />

	<!-- Centered Title -->
	<h1 class="text-center text-2xl font-bold text-gray-800">Hex Squared</h1>

	<!-- Card Component -->
	<Card class="mt-4 w-full max-w-md rounded-lg bg-white p-6 shadow-md">
		<CardHeader>
			<CardTitle class="text-lg font-semibold text-gray-800">Start Game</CardTitle>
		</CardHeader>
		<CardContent>
			<div class="flex flex-col space-y-4 text-sm text-gray-700">
				<p>Choose how you want to play the game.</p>
				<p>You can always send the link to your friends for them to watch.</p>
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
		</CardContent>
		<CardFooter class="mt-4 flex space-x-2">
			<Button
				variant="secondary"
				class="w-full bg-gray-200 text-gray-800 hover:bg-gray-300"
				on:click={() => (window.location.href = link || '')}
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
		class="mt-6 flex items-center text-gray-600 hover:text-gray-900"
	>
		<Github alt="GitHub" class="mr-2 h-6 w-6" />
		<span>View on GitHub</span>
	</a>
</div>
