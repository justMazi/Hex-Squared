import numpy as np
import matplotlib.pyplot as plt
import matplotlib.patches as patches
from collections import deque
import json

# Global neighbor offsets for faster computation
NEIGHBOR_OFFSETS = np.array([
    [-1, 1, 0], [1, -1, 0], [0, -1, 1],
    [0, 1, -1], [-1, 0, 1], [1, 0, -1]
])

class MyState:
    def __init__(self, board: np.ndarray, player: int):
        """
        Initialize the Hex game state.
        :param board: Numpy array representing the game board.
        :param player: Current player's turn (1, 2, or 3).
        """
        self.board = board
        self.radius = (len(board) - 1) // 2
        self.player = player
        self.neighbors_cache = self._precompute_neighbors()

    def _precompute_neighbors(self):
        """
        Precompute neighbors for each hex cell.
        """
        neighbors = {}
        for coord in self.board[:, :3]:
            potential_neighbors = coord + NEIGHBOR_OFFSETS
            valid = np.all(np.abs(potential_neighbors[:, :2]) <= self.radius, axis=1)
            neighbors[tuple(coord)] = [tuple(n) for n in potential_neighbors[valid]]
        return neighbors

    def get_possible_actions(self):
        """
        Get all possible actions (empty cells) using optimized NumPy filtering.
        """
        empty_mask = self.board[:, 4] == 0  # Cells with no owner
        return [tuple(action[:4]) for action in self.board[empty_mask]]  # Return actions as tuples

    def take_action(self, action):
        """
        Apply a move and return a new game state using NumPy array modification.
        :param action: Tuple of (r, s, q, index).
        """
        r, s, q, index = action
        new_board = self.board.copy()
        idx = np.where((new_board[:, :3] == [r, s, q]).all(axis=1))[0][0]
        new_board[idx, 4] = self.player
        next_player = (self.player % 3) + 1
        return MyState(new_board, next_player)

    def undo_action(self, action):
        """
        Undo a move to revert to the previous state.
        :param action: Tuple of (r, s, q, index).
        """
        r, s, q, index = action
        idx = np.where((self.board[:, :3] == [r, s, q]).all(axis=1))[0][0]
        self.board[idx, 4] = 0
        self.player = (self.player - 2) % 3 + 1

    def is_terminal(self, verbose=True):
        """
        Check if the game state is terminal (a player has won or no moves are left).
        """
        if len(self.get_possible_actions()) == 0:
            if verbose:
                print("DRAW!")
            return True

        return self.has_won(1) or self.has_won(2) or self.has_won(3)

    def get_reward(self):
        """
        Get the reward for the current state.
        """
        if self.has_won(self.player):
            print("WIN")
            return 1.0  # Current player wins
        elif self.has_won((self.player % 3) + 1):
            print("lose")
            return -1.0  # Opponent wins
        print("nic")
        return 0.0  # No winner yet
    

    def get_current_player(self):
        """
        Get the current player.
        """
        return self.player

    def has_won(self, player):
        """
        Optimized winning check using stack-based DFS.
        """
        visited = set()
        stack = [
            tuple(coord) for coord in self.board[(self.board[:, 4] == player) & self._is_starting_edge_mask(player), :3]
        ]

        while stack:
            current = stack.pop()
            if current in visited:
                continue
            visited.add(current)
            if self._is_opposite_edge(current, player):
                return True
            stack.extend(
                tuple(neigh) for neigh in self.neighbors_cache.get(current, [])
                if tuple(neigh) not in visited and self._cell_owner(neigh) == player
            )
        return False

    def _is_starting_edge_mask(self, player):
        """
        Get a boolean mask for cells on the starting edge for a player.
        """
        return {
            1: self.board[:, 2] == -self.radius,
            2: self.board[:, 0] == -self.radius,
            3: self.board[:, 1] == -self.radius
        }[player]

    def _is_opposite_edge(self, coord, player):
        """
        Check if a hex is on the opposite edge for a player.
        """
        r, s, q = coord
        return {1: q == self.radius, 2: r == self.radius, 3: s == self.radius}[player]

    def _cell_owner(self, coord):
        """
        Get the owner of a specific cell.
        """
        idx = np.where((self.board[:, :3] == coord).all(axis=1))[0]
        return self.board[idx, 4][0] if idx.size > 0 else -1


def load_game_state(file_path: str) -> dict:
    """
    Load game state JSON from a file.
    """
    with open(file_path, 'r') as file:
        return json.load(file)


def create_hex_state_from_json(json_data: dict) -> MyState:
    """
    Create a HexState instance from JSON data.
    :param json_data: Parsed JSON data.
    :return: MyState instance.
    """
    board_data = json_data["board"]
    player = json_data["player"]

    # Convert the board into a numpy array
    board_array = np.array(
        [[hex["R"], hex["S"], hex["Q"], hex["Index"], hex["Owner"]] for hex in board_data]
    )

    return MyState(board=board_array, player=player)


def plot_hex_board_final(board: np.ndarray):
    """
    Visualize the hexagonal board with correct orientation, flipped vertically,
    and each hexagon rotated by 90 degrees.
    """
    radius = 11
    fig, ax = plt.subplots(figsize=(10, 10))

    owner_colors = {
        0: 'lightgray',
        1: 'red',
        2: 'green',
        3: 'blue',
    }

    def hex_corners(center_x, center_y, size=1):
        return [
            (
                center_x + size * np.cos(np.radians(90) + np.radians(60) * i),
                center_y + size * np.sin(np.radians(90) + np.radians(60) * i)
            )
            for i in range(6)
        ]

    for cell in board:
        r, s, q, index, owner = cell
        x = np.sqrt(3) * (q + r / 2)
        y = -3 / 2 * r
        hex_color = owner_colors.get(owner, 'black')
        hex_patch = patches.Polygon(hex_corners(x, y, size=0.95), closed=True,
                                     edgecolor='black', facecolor=hex_color)
        ax.add_patch(hex_patch)
        ax.text(x, y, str(index), ha='center', va='center', fontsize=8, color='black')

    ax.set_aspect('equal')
    ax.axis('off')
    ax.set_xlim(-radius * 2, radius * 2)
    ax.set_ylim(-radius * np.sqrt(3), radius * np.sqrt(3))
    plt.show()


if __name__ == "__main__":
    file_path = "V:/MFF/bakalarka/gamestate.json"
    game_data = load_game_state(file_path)
    initial_state = create_hex_state_from_json(game_data)

    # Assuming MCTS is implemented in `mcts.searcher.mcts`
    from mcts.searcher.mcts import MCTS
    searcher = MCTS(time_limit=3000)

    best_action = searcher.search(initial_state=initial_state)
    print(f"Best Action: {best_action}")

    new_state = initial_state.take_action(best_action)
    plot_hex_board_final(new_state.board)
