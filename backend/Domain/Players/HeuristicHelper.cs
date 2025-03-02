namespace Domain;

public static class HeuristicHelper
{
    public static Task<int> RandomPlayer(Game game, int playerNum, CancellationToken cancellationToken)
    {
        return Task.FromResult(game.PlayableHexagons[Random.Shared.Next(game.PlayableHexagons.Count)].Index);
    }


    #region PathFinderHeuristic  
    
    public static Task<int> PathFinderHeuristic(Game game, int playerNum, CancellationToken cancellationToken)
    {
        
        var radius = game.Radius; 

        // Get the starting and target edges
        var startingEdge = GetStartingEdge(game.Hexagons, playerNum, radius);
        var targetEdge = GetTargetEdge(game.Hexagons, playerNum, radius);

        // Attempt to find the shortest path
        var shortestPath = FindShortestPath(game.Hexagons, startingEdge, targetEdge, playerNum);

        // If a path exists, pick a hexagon near the middle
        if (shortestPath != null && shortestPath.Count > 0)
        {
            // Find the closest playable hexagon to the middle of the path
            var middleIndex = shortestPath.Count / 2;
            var playableHex = shortestPath
                .OrderBy(h => Math.Abs(shortestPath.IndexOf(h) - middleIndex)) // Prefer hexes near the middle
                .FirstOrDefault(h => !h.IsTaken && !h.IsTaken);
            // If a playable hexagon is found, return its index
            if (playableHex != null)
            {
                return Task.FromResult(playableHex.Index);
            }
        }

        // If no path exists, prioritize moves closer to the starting edge
        var bestMove = game.PlayableHexagons
            .OrderBy(h => startingEdge.Select(edge => HexDistance(h, edge)).Min())
            .First();

        return Task.FromResult(bestMove.Index);
    }

    private static List<Hex>? FindShortestPath(IEnumerable<Hex> hexagons, IEnumerable<Hex> startingEdge, IEnumerable<Hex> targetEdge, int playerNum)
    {
        var visited = new HashSet<Hex>();
        var priorityQueue = new PriorityQueue<(Hex hex, List<Hex> path), int>();

        // Add all starting edge hexagons to the queue
        foreach (var start in startingEdge)
        {
            var initialPath = new List<Hex> { start };
            var heuristicCost = HexDistanceToCenter(start) + HexDistanceToTargetEdge(start, targetEdge);
            priorityQueue.Enqueue((start, initialPath), heuristicCost);
            visited.Add(start);
        }

        // Perform weighted BFS (A*-like)
        while (priorityQueue.Count > 0)
        {
            var (current, path) = priorityQueue.Dequeue();

            // Check if the current hex is part of the target edge
            if (targetEdge.Contains(current))
            {
                return path;
            }

            // Add neighbors to the queue
            foreach (var neighbor in GetNeighbors(hexagons, current))
            {
                if (!visited.Contains(neighbor) && (!neighbor.IsTaken || neighbor.Owner == playerNum))
                {
                    visited.Add(neighbor);

                    var newPath = new List<Hex>(path) { neighbor };
                    var heuristicCost = HexDistanceToCenter(neighbor) + HexDistanceToTargetEdge(neighbor, targetEdge);

                    priorityQueue.Enqueue((neighbor, newPath), heuristicCost);
                }
            }
        }

        return null; // No path found
    }

    private static IEnumerable<Hex> GetStartingEdge(IEnumerable<Hex> hexagons, int playerNum, int radius)
    {
        return hexagons.Where(h => playerNum switch
        {
            1 => h.Q == -radius, // Left edge for Player 1
            2 => h.R == -radius, // Top edge for Player 2
            3 => h.S == -radius, // Top-right edge for Player 3
            _ => throw new ArgumentException("Invalid player number")
        });
    }

    private static IEnumerable<Hex> GetTargetEdge(IEnumerable<Hex> hexagons, int playerNum, int radius)
    {
        return hexagons.Where(h => playerNum switch
        {
            1 => h.Q == radius,  // Right edge for Player 1
            2 => h.R == radius,  // Bottom edge for Player 2
            3 => h.S == radius,  // Bottom-left edge for Player 3
            _ => throw new ArgumentException("Invalid player number")
        });
    }

    private static IEnumerable<Hex> GetNeighbors(IEnumerable<Hex> hexagons, Hex hex)
    {
        // Define the six possible directions in a hex grid
        var directions = new[]
        {
            (1, -1, 0), (-1, 1, 0), (0, 1, -1), (0, -1, 1), (1, 0, -1), (-1, 0, 1)
        };

        foreach (var (dq, dr, ds) in directions)
        {
            var neighbor = hexagons.FirstOrDefault(h => h.Q == hex.Q + dq && h.R == hex.R + dr && h.S == hex.S + ds);
            if (neighbor != null)
            {
                yield return neighbor;
            }
        }
    }

    private static int HexDistanceToCenter(Hex hex)
    {
        return Math.Max(Math.Abs(hex.Q), Math.Max(Math.Abs(hex.R), Math.Abs(hex.S)));
    }

    private static int HexDistance(Hex a, Hex b)
    {
        return Math.Max(
            Math.Abs(a.Q - b.Q),
            Math.Max(
                Math.Abs(a.R - b.R),
                Math.Abs(a.S - b.S)
            )
        );
    }

    private static int HexDistanceToTargetEdge(Hex hex, IEnumerable<Hex> targetEdge)
    {
        return targetEdge
            .Select(target => HexDistance(hex, target))
            .Min();
    }
    
    #endregion


    public static Task<int> EdgeControlHeuristic(Game game, int playerNum, CancellationToken cancellationToken)
    {
        var bestMove = game.PlayableHexagons
            .OrderByDescending(h => Math.Max(Math.Abs(h.Q), Math.Max(Math.Abs(h.R), Math.Abs(h.S))))
            .First();
        return Task.FromResult(bestMove.Index);    }

    public static Task<int> CenterControlHeuristic(Game game,  int playerNum, CancellationToken cancellationToken)
    {
        var bestMove = game.PlayableHexagons
            .OrderBy(h => Math.Abs(h.Q) + Math.Abs(h.R) + Math.Abs(h.S))
            .First();
        return Task.FromResult(bestMove.Index);    }
}