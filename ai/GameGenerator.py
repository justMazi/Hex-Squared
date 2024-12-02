import numpy as np
from collections import deque
from typing import List, Optional
import asyncio
from fastapi import FastAPI
from pydantic import BaseModel
from typing import List
from http.server import HTTPServer, BaseHTTPRequestHandler
from fastapi.responses import JSONResponse
import json

# HexState and related functionality
import numpy as np
from typing import List, Optional
from collections import deque
import numpy as np
from typing import List, Optional
from collections import deque

class HexState:
    def __init__(self, board: np.ndarray, player: int):
        """
        Initialize the HexState.
        :param board: Numpy array representing the game board.
        :param player: Current player's turn (e.g., 1 or 2).
        """
        self.board = board
        self.player = player

    def get_neighbors(self, coords: tuple) -> List[tuple]:
        """
        Get the neighboring hexes for a given hex coordinate.
        :param coords: Tuple (r, s, q) representing a hex coordinate.
        :return: List of neighbor coordinates as tuples.
        """
        neighbors = []
        r, s, q = coords
        for dr, ds, dq in [(1, -1, 0), (-1, 1, 0), (0, 1, -1),
                           (0, -1, 1), (1, 0, -1), (-1, 0, 1)]:
            neighbor = (r + dr, s + ds, q + dq)
            if any((neighbor == tuple(hex[:3])) for hex in self.board):
                neighbors.append(neighbor)
        return neighbors

    def get_legal_moves(self) -> List[int]:
        """
        Get the list of legal moves.
        :return: List of indices of legal moves.
        """
        radius = 11  # Define the radius of the hex board
        return [
            hex[3] for hex in self.board
            if hex[4] == 0 and not (
                hex[0] == -radius or hex[1] == -radius or hex[2] == -radius or
                hex[0] == radius or hex[1] == radius or hex[2] == radius
            )
        ]

    def apply_move(self, move: int) -> "HexState":
        """
        Apply a move and return a new HexState.
        :param move: Index of the move to apply.
        :return: New HexState after the move is applied.
        """
        new_board = self.board.copy()
        new_board[move][4] = self.player
        next_player = 1 if self.player == 2 else 2

        return HexState(new_board, next_player)

    def try_set_win_state(self, player: int) -> bool:
        """
        Check if a player has won the game.
        :param player: Player to check for a winning state.
        :return: True if the player has won, False otherwise.
        """
        hex_dict = {tuple(hex[:3]): hex for hex in self.board}
        visited = set()
        to_visit = deque()

        # Add starting edges to visit queue
        for hex in self.board:
            if hex[4] == player and self.is_starting_edge(player, hex):
                coords = tuple(hex[:3])
                to_visit.append(coords)
                visited.add(coords)

        # BFS to find a path to the opposite edge
        while to_visit:
            current = to_visit.popleft()
            if self.is_opposite_edge(player, current):
                return True
            for neighbor_coords in self.get_neighbors(current):
                if neighbor_coords in hex_dict:
                    neighbor = hex_dict[neighbor_coords]
                    if neighbor_coords not in visited and neighbor[4] == player:
                        to_visit.append(neighbor_coords)
                        visited.add(neighbor_coords)

        return False

    def is_starting_edge(self, player: int, hex: np.ndarray, radius: int = 11) -> bool:
        """
        Check if a hex is on the starting edge for a player.
        :param player: Player number.
        :param hex: Hex array.
        :param radius: Radius of the board.
        :return: True if the hex is on the starting edge.
        """
        r, s, q = hex[:3]
        return {1: q == -radius, 2: r == -radius}[player]

    def is_opposite_edge(self, player: int, hex: tuple, radius: int = 11) -> bool:
        """
        Check if a hex is on the opposite edge for a player.
        :param player: Player number.
        :param hex: Hex coordinates as a tuple.
        :param radius: Radius of the board.
        :return: True if the hex is on the opposite edge.
        """
        r, s, q = hex
        return {1: q == radius, 2: r == radius}[player]

    def is_terminal(self) -> bool:
        """
        Check if the game state is terminal (no more legal moves).
        :return: True if the game is terminal, False otherwise.
        """
        return len(self.get_legal_moves()) == 0

    def get_winner(self) -> Optional[int]:
        """
        Determine the winner of the game.
        :return: Player number if there's a winner, None otherwise.
        """
        for player in [1, 2]:
            if self.try_set_win_state(player):
                return player
        return None

from pydantic import BaseModel

class Hex(BaseModel):
    R: int
    S: int
    Q: int
    Index: int
    Owner: int
