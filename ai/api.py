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

from GameGenerator import Hex, HexState


app = FastAPI()

from pydantic import BaseModel
from typing import List



class MoveRequest(BaseModel):
    board: List[Hex]  # List of Hex dictionaries
    player: int
    iter_limit: int = 10
    num_threads: int = 4

@app.post("/best-move/")
async def get_best_move(request: MoveRequest):
    # Convert the list of Hex objects to a NumPy array for HexState
    board_array = np.array(
        [[h.R, h.S, h.Q, h.Index, h.Owner] for h in request.board],
        dtype=int
    )
    
    # Initialize HexState
    initial_state = HexState(board_array, player=request.player)

    # Initialize Monte Carlo tree
    montecarlo = initialize_montecarlo(initial_state)

    # Run simulations
    montecarlo.simulate(request.iter_limit)

    # Select the best move
    chosen_child_node = montecarlo.make_choice()

    # Extract the index of the chosen move
    chosen_move_index = None
    for hexagon in chosen_child_node.state.board:
        if hexagon[4] != 0 and initial_state.board[hexagon[3]][4] == 0:
            chosen_move_index = hexagon[3]
            break

    if chosen_move_index is None:
        return JSONResponse(status_code=500, content={"error": "No valid move found."})

    return {"BestMove": chosen_move_index}


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



from montecarlo.node import Node
from montecarlo.montecarlo import MonteCarlo
import numpy as np
import random

# Define the child finder function
def child_finder(node, _):
    state = node.state  # Extract the HexState object from the node
    for move in state.get_legal_moves():
        child_state = state.apply_move(move)  # Generate a new state for each move
        child_node = Node(child_state)  # Create a new node for the child state
        child_node.player_number = child_state.player  # Set the player for the child node
        node.add_child(child_node)  # Add the child node to the parent node


# Define the node evaluator function
def node_evaluator(node, _):
    state = node.state  # Extract the HexState object from the node
    winner = state.get_winner()
    if winner == state.player:
        return 1  # Current player wins
    elif winner is not None:
        return -1  # Opponent wins
    return 0  # Not a terminal state

# Create the Monte Carlo tree for HexState
def initialize_montecarlo(hex_state: HexState):
    root_node = Node(hex_state)
    root_node.player_number = hex_state.player
    montecarlo = MonteCarlo(root_node)
    montecarlo.child_finder = child_finder
    montecarlo.node_evaluator = node_evaluator
    return montecarlo


# Example usage
if __name__ == "__main__":
    server = HTTPServer(("localhost", 8000), FastAPIRequestHandler)
    print("Server running on http://localhost:8000")
    server.serve_forever()
