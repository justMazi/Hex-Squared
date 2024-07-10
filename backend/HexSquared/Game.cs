namespace HexSquared;

public class Game(string gameCode, int radius = 9)
{
    public IPlayer[] Players = new IPlayer[3];
    public readonly List<Hex> Hexagons = GameHelpers.GenerateInnerHexagonCoordinates(radius);
    public readonly List<int> NonReservedColors = [1,2,3];
    public CurrentMovePlayerIndex _currentMovePlayerIndex = new(1);
    public State State = State.PlayerGather;
    public readonly string GameCode = gameCode;
    private readonly int Radius = radius; 
    public bool TryMove(int player, int index)
    {
        // Game is not even running
        if (State is not State.InProgress)
        {
            return false;
        }
        
        // its not the players turn
        if (player != _currentMovePlayerIndex)
        {
            return false;
        }
        
        var hex = Hexagons.Find(h => h.Index == index);
        
        // the clicked hex field is already owned by someone else
        if (hex.Player != 0)
        {
            return false;
        }
        
        hex.SetPlayer(player);
        _currentMovePlayerIndex.Increment();
        return true;
    }
    
    public void ReserveColor(int number, bool isAiPlayer)
    {
        NonReservedColors.Remove(number);
        Players[number-1] = isAiPlayer ? new AIPlayer() : new HumanPlayer();
        if (NonReservedColors.Count is 0)
        {
            State = State.InProgress;
        }    
    }

    public bool IsCurrentMoveArtificial()
    {
        return (Players[_currentMovePlayerIndex - 1] as AIPlayer) is not null;
    }
    
    public bool CheckWin(int player)
    {
        HashSet<Hex> visited = new HashSet<Hex>();
        Queue<Hex> toVisit = new Queue<Hex>();

        // Add all starting hexes to the queue (e.g., left edge for player 1)
        var filtered = Hexagons.Where(h => h.Player == player);
        foreach (var hex in filtered)
        {
            if (IsStartingEdge(hex, player))
            {
                toVisit.Enqueue(hex);
                visited.Add(hex);
            }
        }

        while (toVisit.Count > 0)
        {
            var current = toVisit.Dequeue();

            // Check if the current hex is on the opposite edge
            if (IsOppositeEdge(current, player))
            {
                State = State.Finished;
                return true;
            }

            // Add all unvisited neighbors controlled by the same player
            foreach (var neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor) && neighbor.Player == player)
                {
                    toVisit.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        return false;
    }
    
    private bool IsStartingEdge(Hex hex, int player)
    {
        // Define the starting edge condition for the player
        // Example: left edge for player 1, top edge for player 2
        // Adjust this logic based on your game rules
        return hex.R == -Radius;
    }

    private bool IsOppositeEdge(Hex hex, int player)
    {
        // Define the opposite edge condition for the player
        // Example: right edge for player 1, bottom edge for player 2
        // Adjust this logic based on your game rules
        return hex.R == Radius;
    }

    private IEnumerable<Hex> GetNeighbors(Hex hex)
    {
        // Define the six possible directions in a hex grid
        var directions = new (int, int, int)[]
        {
            (1, -1, 0), (-1, 1, 0), (0, 1, -1), (0, -1, 1), (1, 0, -1), (-1, 0, 1)
        };

        foreach (var (dr, dq, ds) in directions)
        {
            var neighbor = Hexagons.Find(h => h.R == hex.R + dr && h.Q == hex.Q + dq && h.S == hex.S + ds);
            if (neighbor != null)
            {
                yield return neighbor;
            }
        }
    }
}


