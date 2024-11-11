<script lang="ts">
	import { onMount } from 'svelte';
	import { Card, CardContent, CardHeader, CardTitle, CardFooter } from '$lib/components/ui/card';
	import { Copy, Dice6 } from 'lucide-svelte';
	import { Button } from '$lib/components/ui/button';

	let gameCode = generateGameCode();
	let link: string | null = null;

	onMount(() => {
		if (typeof window !== 'undefined') {
			link = `${window.location.origin}/${gameCode}`;
		}
	});

	const copyToClipboard = () => {
		if (link) navigator.clipboard.writeText(link);
	};

	function generateGameCode() {
		return Math.random().toString(36).substring(2, 8);
	}

	function regenerateGameCode() {
		gameCode = generateGameCode();
		if (typeof window !== 'undefined') {
			link = `${window.location.origin}/${gameCode}`;
		}
	}
</script>

<div class="flex min-h-screen items-center justify-center bg-gray-100">
	<Card class="w-full max-w-md rounded-lg bg-white p-6 shadow-md">
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
			<!-- 
            <Button variant="secondary" class="w-1/2 bg-gray-200 text-gray-800 hover:bg-gray-300"
            >Locally</Button
			>
            -->
			<Button
				variant="secondary"
				class="w-full bg-gray-200 text-gray-800 hover:bg-gray-300"
				on:click={() => (window.location.href = link || '')}>Enter the game</Button
			>
		</CardFooter>
	</Card>
</div>
