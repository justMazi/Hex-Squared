import numpy as np
from collections import deque
from typing import List, Optional
from pydantic import BaseModel
from typing import List
import numpy as np
from typing import List, Optional
from collections import deque
import numpy as np
from typing import List, Optional
from collections import deque
from collections import deque
from typing import List, Optional
import numpy as np
class HexState:
    def __init__(self, board: np.ndarray, player: int):
        """
        Initialize the HexState.
        :param board: Numpy array representing the game board.
        :param player: Current player's turn (1, 2, or 3).
        """
        self.board = board
        self.player = player
        self.hex_dict = {tuple(hex[:3]): hex for hex in self.board}  # Precompute for fast access

    def get_legal_moves(self) -> List[int]:
        """
        Get the list of legal moves for a given player.
        :param for_player: Player number (1, 2, or 3). If None, defaults to current player.
        :return: List of indices of legal moves.
        """

        radius = 11
        return [
            hex[3] for hex in self.board
            if hex[4] == 0 and not any(
                coord == radius or coord == -radius for coord in hex[:3]
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
        next_player = (self.player % 3) + 1  # Rotate among 1, 2, 3
        return HexState(new_board, next_player)

    def try_set_win_state(self, player: int) -> bool:
        """
        Check if a player has won the game.
        :param player: Player to check for a winning state.
        :return: True if the player has won, False otherwise.
        """
        visited = set()
        to_visit = deque(
            tuple(hex[:3]) for hex in self.board
            if hex[4] == player and self.is_starting_edge(player, hex)
        )

        while to_visit:
            current = to_visit.popleft()
            if current in visited:
                continue
            visited.add(current)
            if self.is_opposite_edge(player, current):
                return True
            to_visit.extend(
                neighbor for neighbor in self.get_neighbors(current)
                if neighbor not in visited and self.hex_dict[neighbor][4] == player
            )

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
        return {
            1: q == -radius,  # Player 1's starting edge
            2: r == -radius,  # Player 2's starting edge
            3: s == -radius   # Player 3's starting edge
        }[player]

    def is_opposite_edge(self, player: int, hex: tuple, radius: int = 11) -> bool:
        """
        Check if a hex is on the opposite edge for a player.
        :param player: Player number.
        :param hex: Hex coordinates as a tuple.
        :param radius: Radius of the board.
        :return: True if the hex is on the opposite edge.
        """
        r, s, q = hex
        return {
            1: q == radius,   # Player 1's opposite edge
            2: r == radius,   # Player 2's opposite edge
            3: s == radius    # Player 3's opposite edge
        }[player]

    def is_terminal(self) -> bool:
        """
        Check if the game state is terminal (no more legal moves).
        :return: True if the game is terminal, False otherwise.
        """
        return not self.get_legal_moves()

    def get_winner(self) -> Optional[int]:
        """
        Determine the winner of the game.
        :return: Player number if there's a winner, None otherwise.
        """
        for player in [1, 2, 3]:
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
