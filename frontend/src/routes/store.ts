import { writable } from 'svelte/store';

// Initial game state
export const initialGameState = {
	currentPlayer: 'player1',
	hexTiles: [],
	status: 'playing',
	turn: 1,
	winner: null
};

// Create a writable store
export const gameState = writable(initialGameState);

// Function to toggle the player
export const togglePlayer = () => {
	gameState.update((state) => ({
		...state,
		currentPlayer: state.currentPlayer === 'player1' ? 'player2' : 'player1',
		turn: state.turn + 1
	}));
};

// Function to update a specific hex tile's state
export const updateHexTileState = (q, r, newState) => {
	gameState.update((state) => {
		const updatedTiles = state.hexTiles.map((tile) =>
			tile.q === q && tile.r === r ? { ...tile, ...newState } : tile
		);
		return { ...state, hexTiles: updatedTiles };
	});
};

// Function to check for a winner (simple condition for example)
export const checkWinner = () => {
	gameState.update((state) => {
		const player1Tiles = state.hexTiles.filter((tile) => tile.owner === 'player1');
		const player2Tiles = state.hexTiles.filter((tile) => tile.owner === 'player2');

		if (player1Tiles.length > 10) {
			return { ...state, status: 'finished', winner: 'player1' };
		} else if (player2Tiles.length > 10) {
			return { ...state, status: 'finished', winner: 'player2' };
		}

		return state; // No winner yet
	});
};
