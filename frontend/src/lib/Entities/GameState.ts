import type { Player } from './Owner';

export type Game = {
	id: string;
	status: GameStatus;
	time: Date;
};

export type GameState = {
	currentPlayer: Player;
	hexTiles: Hex[];
	status: GameStatus;
	turn: 1;
};
export type Hex = {
	coords: HexCoordinates;
	owner: Player;
};

export type HexCoordinates = {
	q: number;
	r: number;
	s: number;
};

export enum GameStatus {
	WaitingForPlayer = 0,
	InProgress = 1,
	Finished = 2
}
