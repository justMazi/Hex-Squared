using System.Collections.Immutable;
using Domain.Players;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Domain;

public record Game(
    GameId Id,
    IPlayer[] Players,
    IReadOnlyList<Hex> Hexagons,
    CurrentMovePlayerIndex CurrentMovePlayerIndex,
    GameState GameState)
{
    public Game(GameId id)
        : this(
            id,
            Players: new IPlayer[3],
            Hexagons: GameHelpers.GenerateInnerHexagonCoordinates(10),
            CurrentMovePlayerIndex: new CurrentMovePlayerIndex(0),
            GameState: GameState.WaitingForPlayers)
    {
    }

    public Option<Game> PickColor(IPlayer player, int color)
    {
        if (!Players.Contains(null))
            return None;

        var updatedPlayers = Players.ToArray();
        if (updatedPlayers[color] is not null) return None;
        updatedPlayers[color] = player;

        var isNoColorFree = updatedPlayers.All(p => p != null);
        var newGameState = isNoColorFree ? GameState.InProgress : GameState.WaitingForPlayers;
        var newIndex = new CurrentMovePlayerIndex(1);

        return Some(this with
        {
            Players = updatedPlayers,
            GameState = newGameState,
            CurrentMovePlayerIndex = newIndex
        });
    }

    public Option<Game> UnpickColor(int color)
    {
        var updatedPlayers = Players.ToArray();
        updatedPlayers[color] = null;

        return Some(this with
        {
            Players = updatedPlayers
        });    
    }

    public Option<Game> Move(IPlayer player, int index)
    {
        if (GameState is not GameState.InProgress || player.PlayerNum != CurrentMovePlayerIndex.Value)
            return None;

        var hex = Hexagons.FirstOrDefault(h => h.Index == index);
        if (hex == null || hex.IsTaken)
            return None;

        var updatedHexagons = Hexagons
            .Select(h => h.Index == index ? h.SetPlayer(player.PlayerNum) : h)
            .ToImmutableList();

        var newPlayerIndex = CurrentMovePlayerIndex.Increment();

        return Some(this with
        {
            Hexagons = updatedHexagons,
            CurrentMovePlayerIndex = newPlayerIndex
        });
    }
    
    public Option<Game> TrySetWinState(IPlayer player)
    {
        System.Collections.Generic.HashSet<Hex> visited = new();
        Queue<Hex> toVisit = new();

        // Add all starting hexes to the queue (e.g., left edge for player 1)
        var startingHexes = Hexagons.Where(h => h.Owner == player.PlayerNum && IsStartingEdge(player, h));
        foreach (var hex in startingHexes)
        {
            toVisit.Enqueue(hex);
            visited.Add(hex);
        }

        while (toVisit.Count > 0)
        {
            var current = toVisit.Dequeue();

            if (IsOppositeEdge(player, current))
            {
                // Return a new Game instance with GameState set to Finished if a win is detected
                return Some(this with { GameState = GameState.Finished });
            }

            // Add all unvisited neighbors controlled by the same player
            foreach (var neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor) && neighbor.Owner == player.PlayerNum)
                {
                    toVisit.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        return None;
    }

    private bool IsStartingEdge(IPlayer player, Hex hex, int radius = 11)
    {
        return player.PlayerNum switch
        {
            1 => hex.Q == -radius,          // Player 1 starts on the left edge (q == -radius)
            2 => hex.R == -radius,          // Player 2 starts on the top edge (r == -radius)
            3 => hex.S == -radius,          // Player 3 starts on the top-right edge (s == -radius)
            _ => throw new ArgumentException("Invalid player number")
        };
    }

    private bool IsOppositeEdge(IPlayer player, Hex hex, int radius = 11)
    {
        return player.PlayerNum switch
        {
            1 => hex.Q == radius,           // Player 1 needs to connect to the right edge (q == radius)
            2 => hex.R == radius,           // Player 2 needs to connect to the bottom edge (r == radius)
            3 => hex.S == radius,           // Player 3 needs to connect to the bottom-left edge (s == radius)
            _ => throw new ArgumentException("Invalid player number")
        };
    }

    private IEnumerable<Hex> GetNeighbors(Hex hex)
    {
        // Define the six possible directions in a hex grid
        var directions = new[]
        {
            (1, -1, 0), (-1, 1, 0), (0, 1, -1), (0, -1, 1), (1, 0, -1), (-1, 0, 1)
        };

        foreach (var (dr, dq, ds) in directions)
        {
            var neighbor = Hexagons.FirstOrDefault(h => h.R == hex.R + dr && h.Q == hex.Q + dq && h.S == hex.S + ds);
            if (neighbor != null)
            {
                yield return neighbor;
            }
        }
    }
}