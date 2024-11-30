import math
import os
import numpy as np
import random
from collections import deque, defaultdict
from typing import List, Optional
from concurrent.futures import ThreadPoolExecutor, as_completed
import asyncio
from fastapi import FastAPI
from pydantic import BaseModel
from typing import List
from http.server import HTTPServer, BaseHTTPRequestHandler
from fastapi.responses import JSONResponse
import json

# HexState and related functionality
class HexState:
    def __init__(self, board: np.ndarray, player: int):
        self.board = board
        self.player = player
        self.neighbor_map = self.precompute_neighbors()

    def precompute_neighbors(self):
        hex_dict = {tuple(hex[:3]): hex for hex in self.board}
        neighbors = {}
        for hex in self.board:
            coords = tuple(hex[:3])
            neighbors[coords] = [
                (coords[0] + dr, coords[1] + ds, coords[2] + dq)
                for dr, ds, dq in [(1, -1, 0), (-1, 1, 0), (0, 1, -1),
                                   (0, -1, 1), (1, 0, -1), (-1, 0, 1)]
                if (coords[0] + dr, coords[1] + ds, coords[2] + dq) in hex_dict
            ]
        return neighbors

    def get_legal_moves(self) -> List[int]:
        radius = 11  # Define the radius of the hex board
        # Exclude edge hexes and return indices of legal moves
        return [
            hex[3] for hex in self.board
            if hex[4] == 0 and not (
                hex[0] == -radius or hex[1] == -radius or hex[2] == -radius or
                hex[0] == radius or hex[1] == radius or hex[2] == radius
            )
        ]

    def apply_move(self, move: int) -> "HexState":
        new_board = self.board.copy()
        new_board[move][4] = self.player
        next_player = 1 if self.player == 2 else 2
        return HexState(new_board, next_player)

    def try_set_win_state(self, player: int) -> bool:
        hex_dict = {tuple(hex[:3]): hex for hex in self.board}
        visited = set()
        to_visit = deque()

        for hex in self.board:
            if hex[4] == player and self.is_starting_edge(player, hex):
                coords = tuple(hex[:3])
                to_visit.append(coords)
                visited.add(coords)

        while to_visit:
            current = to_visit.popleft()
            if self.is_opposite_edge(player, current):
                return True
            for neighbor_coords in self.neighbor_map[current]:
                if neighbor_coords in hex_dict:
                    neighbor = hex_dict[neighbor_coords]
                    if neighbor_coords not in visited and neighbor[4] == player:
                        to_visit.append(neighbor_coords)
                        visited.add(neighbor_coords)

        return False

    def is_starting_edge(self, player: int, hex: np.ndarray, radius: int = 11) -> bool:
        r, s, q = hex[:3]
        return {1: q == -radius, 2: r == -radius}[player]

    def is_opposite_edge(self, player: int, hex: tuple, radius: int = 11) -> bool:
        r, s, q = hex
        return {1: q == radius, 2: r == radius}[player]

    def is_terminal(self) -> bool:
        return self.get_winner() is not None or len(self.get_legal_moves()) == 0

    def get_winner(self) -> Optional[int]:
        for player in [1, 2]:
            if self.try_set_win_state(player):
                return player
        return None

def simulate(state: HexState, player: int) -> int:
    """
    Simulates a game from the given state until a terminal state is reached.
    Returns 1 if the player wins, 0 otherwise.
    """
    current_state = state
    while not current_state.is_terminal():
        legal_moves = current_state.get_legal_moves()
        move = random.choice(legal_moves)
        current_state = current_state.apply_move(move)
    return 1 if current_state.get_winner() == player else 0

import os
from concurrent.futures import ThreadPoolExecutor, as_completed
from collections import defaultdict
from random import choice  # Assuming simulate requires some randomness

def mcts_parallel(initial_state: HexState, iter_limit: int = 100) -> int:
    """
    Parallelized MCTS where each iteration of the simulation runs in a separate thread.
    
    :param initial_state: The starting state of the Hex game.
    :param iter_limit: Total number of MCTS iterations to perform.
    :return: The best move based on MCTS simulations.
    """
    legal_moves = initial_state.get_legal_moves()
    results = defaultdict(int)

    def simulate_one_iteration():
        """
        Simulates one random game starting from the initial state.
        Chooses a random legal move to begin with and simulates its outcome.
        :return: The move that was simulated and whether it resulted in a win.
        """
        move = choice(legal_moves)  # Pick a random move to simulate
        new_state = initial_state.apply_move(move)
        win = simulate(new_state, initial_state.player)  # Perform the simulation
        return move, win

    # Run all iterations in parallel
    with ThreadPoolExecutor(max_workers=os.cpu_count()) as executor:
        futures = [executor.submit(simulate_one_iteration) for _ in range(iter_limit)]
        
        for future in as_completed(futures):
            try:
                move, win = future.result()
                results[move] += win  # Aggregate wins for each move
            except Exception as e:
                print(f"Error in simulation iteration: {e}")

    # Select the move with the highest win count
    best_move = max(results, key=results.get)
    return best_move


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
            coordinates.append([r, s, q, index, 0])
            index += 1
    return np.array(coordinates, dtype=int)

# FastAPI app setup
app = FastAPI()

class MoveRequest(BaseModel):
    board: List[List[int]]
    player: int
    iter_limit: int = 1000
    num_threads: int = 4

@app.post("/best-move/")
async def get_best_move(request: MoveRequest):
    board = np.array(request.board, dtype=int)
    initial_state = HexState(board, player=request.player)
    best_move = mcts_parallel(
        initial_state,
        iter_limit=request.iter_limit,
        num_threads=request.num_threads
    )
    return {"BestMove": int(best_move)}

@app.get("/")
def root():
    return {"message": "Hex MCTS API is running"}

class NumpyEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.integer):
            return int(obj)
        elif isinstance(obj, np.floating):
            return float(obj)
        elif isinstance(obj, np.ndarray):
            return obj.tolist()  # Convert NumPy array to a list
        return super().default(obj)

class FastAPIRequestHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        try:
            route = next((r for r in app.routes if r.path == self.path and r.methods and "GET" in r.methods), None)
            if route:
                response = asyncio.run(route.endpoint())
                self._send_response(response)
            else:
                self.send_error(404, "Not Found")
        except Exception as e:
            self.send_error(500, str(e))

    def do_POST(self):
        try:
            route = next((r for r in app.routes if r.path == self.path and r.methods and "POST" in r.methods), None)
            if route:
                content_length = int(self.headers["Content-Length"])
                body = self.rfile.read(content_length).decode("utf-8")
                request_data = json.loads(body)

                # Prepare a Pydantic model from the request data
                request_model = MoveRequest(**request_data)

                # Run the route's endpoint in an asyncio event loop
                response = asyncio.run(route.endpoint(request_model))
                self._send_response(response)
            else:
                self.send_error(404, "Not Found")
        except Exception as e:
            self.send_error(500, str(e))

    def _send_response(self, response):
        # Serialize the response using the custom encoder
        if isinstance(response, JSONResponse):
            self.send_response(response.status_code)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(response.body)
        else:
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(json.dumps(response, cls=NumpyEncoder).encode("utf-8"))

if __name__ == "__main__":
    server = HTTPServer(("localhost", 8000), FastAPIRequestHandler)
    print("Server running on http://localhost:8000")
    server.serve_forever()
