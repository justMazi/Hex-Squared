import math
import numpy as np
import random
from typing import List
from collections import deque

from typing import List, Optional
import numpy as np
class HexState:
    def __init__(self, board: np.ndarray, player: int):
        """
        Represents the current game state.
        :param board: Numpy array of (r, s, q, index, owner).
        :param player: The current player (1, 2, or 3).
        """
        self.board = board
        self.player = player

    def get_legal_moves(self) -> List[int]:
        """
        Returns the indices of all unowned hex tiles (legal moves).
        """
        return [int(hex[3]) for hex in self.board if hex[4] == 0]

    def apply_move(self, move: int) -> "HexState":
        """
        Applies a move to the board and switches to the next player.
        :param move: The index of the hex to claim.
        :return: A new HexState with the updated board.
        """
        new_board = self.board.copy()
        new_board[move][4] = self.player  # Claim the hex for the current player
        next_player = 1 if self.player == 2 else 2  # Toggle between player 1 and 2
        return HexState(new_board, next_player)

    def is_draw(self, players: List[int]) -> bool:
        """
        Checks if the game is a draw by attempting to fill all remaining tiles with each player's tiles.
        If no player can win by filling all free tiles, the game is a draw.
        """
        for p in players:
            # Simulate filling all free hexes with the current player's tiles
            filled_board = self.board.copy()
            for hex in filled_board:
                if hex[4] == 0:  # If unowned
                    hex[4] = p

            # Check if the player wins after this
            if self.try_set_win_state(filled_board, p):
                return False  # If any player can win, it's not a draw

        return True

    def try_set_win_state(self, board: np.ndarray, player: int) -> bool:
        # Create a dictionary for quick lookups
        hex_dict = {tuple(hex[:3]): hex for hex in board}
        visited = set()
        to_visit = deque()

        # Add all starting hexes to the queue
        for hex in board:
            if hex[4] == player and self.is_starting_edge(player, hex):
                coords = tuple(hex[:3])
                to_visit.append(coords)
                visited.add(coords)

        # Perform BFS
        while to_visit:
            current = to_visit.popleft()

            # Check if the current hex is on the opposite edge
            if self.is_opposite_edge(player, current):
                return True  # Win detected

            # Add unvisited neighbors controlled by the same player
            for dr, ds, dq in [(1, -1, 0), (-1, 1, 0), (0, 1, -1), (0, -1, 1), (1, 0, -1), (-1, 0, 1)]:
                neighbor_coords = (current[0] + dr, current[1] + ds, current[2] + dq)
                if neighbor_coords in hex_dict:
                    neighbor = hex_dict[neighbor_coords]
                    if neighbor_coords not in visited and neighbor[4] == player:
                        to_visit.append(neighbor_coords)
                        visited.add(neighbor_coords)

        return False

    def is_starting_edge(self, player: int, hex: np.ndarray, radius: int = 11) -> bool:
        """
        Checks if a hex is on the player's starting edge.
        :param player: The player to check (1, 2, or 3).
        :param hex: The hex to check (r, s, q, index, owner).
        :param radius: The board radius.
        :return: True if the hex is on the starting edge.
        """
        r, s, q = hex[:3]
        return {
            1: q == -radius,  # Player 1 starts on the left edge
            2: r == -radius,  # Player 2 starts on the top edge
            3: s == -radius   # Player 3 starts on the top-right edge
        }[player]

    def is_opposite_edge(self, player: int, hex: tuple, radius: int = 11) -> bool:
        """
        Checks if a hex is on the player's opposite edge.
        :param player: The player to check (1, 2, or 3).
        :param hex: The hex to check (r, s, q).
        :param radius: The board radius.
        :return: True if the hex is on the opposite edge.
        """
        r, s, q = hex
        return {
            1: q == radius,   # Player 1 connects to the right edge
            2: r == radius,   # Player 2 connects to the bottom edge
            3: s == radius    # Player 3 connects to the bottom-left edge
        }[player]

    def get_neighbors(self, board: np.ndarray, hex: tuple) -> List[np.ndarray]:
        """
        Gets the neighbors of a given hex.
        :param board: The current board state.
        :param hex: The (r, s, q) coordinates of the hex.
        :return: A list of neighbor hexes as numpy arrays.
        """
        r, s, q = hex
        directions = [(1, -1, 0), (-1, 1, 0), (0, 1, -1), (0, -1, 1), (1, 0, -1), (-1, 0, 1)]
        neighbors = []

        for dr, ds, dq in directions:
            neighbor_coords = (r + dr, s + ds, q + dq)
            for neighbor in board:
                if tuple(neighbor[:3]) == neighbor_coords:
                    neighbors.append(neighbor)
                    break

        return neighbors

    def get_winner(self) -> Optional[int]:
        """
        Determines the winner of the game.
        :return: The player number (1, 2, or 3) if a winner exists, or None if no winner.
        """
        for player in [1, 2, 3]:
            if self.try_set_win_state(self.board, player):
                return player  # Return the player who has won
        return None  # No winner yet


    def is_terminal(self) -> bool:
        """
        Determines if the game has reached a terminal state (win or draw).
        :return: True if the game is over; otherwise, False.
        """

        #return self.get_legal_moves() == []

        if self.try_set_win_state(self.board, self.player):
            return True  # Current player won
        if self.is_draw([1, 2, 3]):
            return True  # The game is a draw
        return False




class MCTSNode:
    def __init__(self, state: HexState, parent=None):
        self.state = state
        self.parent = parent
        self.children = []
        self.visits = 0
        self.total_reward = 0

    def is_fully_expanded(self) -> bool:
        """
        Returns True if all legal moves have been expanded.
        """
        return len(self.children) == len(self.state.get_legal_moves())

    def best_child(self, exploration_weight: float = 1.0) -> "MCTSNode":
        """
        Selects the best child node based on UCB1.
        """
        def ucb1(node):
            exploitation = node.total_reward / (node.visits + 1e-6)
            exploration = exploration_weight * math.sqrt(math.log(self.visits + 1) / (node.visits + 1e-6))
            return exploitation + exploration

        return max(self.children, key=ucb1)

    def expand(self):
        """
        Expands the node by creating a child for an untried move.
        """
        tried_moves = {child.state.board.tobytes() for child in self.children}
        for move in self.state.get_legal_moves():
            new_state = self.state.apply_move(move)
            if new_state.board.tobytes() not in tried_moves:
                child_node = MCTSNode(new_state, parent=self)
                self.children.append(child_node)
                return child_node
        raise Exception("No valid moves to expand")

    def backpropagate(self, reward: float):
        """
        Updates the node and its ancestors with the simulation result.
        """
        self.visits += 1
        self.total_reward += reward
        if self.parent:
            self.parent.backpropagate(reward)


def mcts(initial_state: HexState, iter_limit: int = 10, exploration_weight: float = 1.0) -> int:
    """
    Performs MCTS to find the best move.
    :param initial_state: The initial game state.
    :param iter_limit: The number of iterations to perform.
    :param exploration_weight: UCB1 exploration weight.
    :return: The index of the best move.
    """
    root = MCTSNode(initial_state)

    for _ in range(iter_limit):
        # Selection
        node = root
        while not node.state.is_terminal() and node.is_fully_expanded():
            node = node.best_child(exploration_weight)

        # Expansion
        if not node.state.is_terminal():
            node = node.expand()

        # Simulation
        current_state = node.state
        while not current_state.is_terminal():
            move = random.choice(current_state.get_legal_moves())
            current_state = current_state.apply_move(move)
        winner = current_state.get_winner()

        print(f"Simulated game result: {'Win' if winner == initial_state.player else 'Loss'}")
        print(f"Player {winner} won the game")


        # Backpropagation
        reward = 1 if winner == initial_state.player else 0
        print("Backpropagating reward")
        node.backpropagate(reward)

    # Return the best move
    return root.best_child(exploration_weight=0).state.board


# Example usage
def create_initial_board(radius: int) -> np.ndarray:
    """
    Create the initial game board as a numpy array.
    """
    radius += 1
    coordinates = []

    index = 0
    for r in range(-radius, radius + 1):
        r1 = max(-radius, -r - radius)
        r2 = min(radius, -r + radius)

        for q in range(r1, r2 + 1):
            s = -r - q
            owner = 0  # Default owner for inner tiles

            # Determine edge ownership
            is_red_edge = abs(q) == radius
            is_green_edge = abs(r) == radius
            is_blue_edge = abs(s) == radius

            # Check if the hex is a corner hex (on two edges)
            is_corner_hex = (is_red_edge and is_green_edge) or \
                            (is_green_edge and is_blue_edge) or \
                            (is_blue_edge and is_red_edge)

            # Assign ownership based on edge, but exclude corners
            if not is_corner_hex:
                if is_red_edge:
                    owner = 1  # Red player
                elif is_green_edge:
                    owner = 2  # Green player
                elif is_blue_edge:
                    owner = 3  # Blue player

            # Append the properties to the list
            coordinates.append([r, s, q, index, owner])
            index += 1

    return np.array(coordinates, dtype=int)

# Initialize the board and perform MCTS
radius = 10
import time

board = create_initial_board(radius)
initial_state = HexState(board, player=1)

start_time = time.time()
best_next_state = mcts(initial_state)
end_time = time.time()

print(best_next_state)
print(f"Time taken for MCTS: {end_time - start_time:.3f} seconds")
