using System.Collections.Immutable;
using Domain.Players;
using Domain.Players.MCTS;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Domain;

public record Game(
    GameId Id,
    Player[] Players,
    IReadOnlyList<Hex> Hexagons,
    CurrentMovePlayerIndex CurrentMovePlayerIndex,
    GameState GameState,
    string? AiType,
    int Radius,
    int? Winner = null
    )
{
    public Game(GameId id, int? radius, Type? aiType)
        : this(
            id,
            Players: new Player[3],
            Hexagons: GameHelpers.GenerateInnerHexagonCoordinates(radius ?? 10),
            CurrentMovePlayerIndex: new CurrentMovePlayerIndex(0),
            GameState: GameState.WaitingForPlayers,
            AiType: aiType.FullName,
            Radius: radius ?? 10)
    {
    }

    public IReadOnlyList<Hex> PlayableHexagons => Hexagons.Where(h => !h.IsTaken && Math.Abs(h.Q) != Radius+1 && Math.Abs(h.R) != Radius+1 && Math.Abs(h.S) != Radius+1).ToList();
    public DateTime LastChange = DateTime.Now;
    
    public Option<Game> FillWithAi()
    {
        if (GameState is not GameState.WaitingForPlayers)
        {
            return None;
        }

        var type = Type.GetType(AiType);
        
        return this with
        {
            Players = Players
                .Select((p, index) => p ?? Activator.CreateInstance(type, index + 1) as Player)
                .ToArray(),
            GameState = GameState.InProgress,
            CurrentMovePlayerIndex = new CurrentMovePlayerIndex(1)
        };
    }

    
    public Option<Game> PickColor(Player player, int color)
    {
        if (!Players.Contains(null))
            return None;

        var updatedPlayers = Players.ToArray();
        if (updatedPlayers[color - 1] is not null) return None;
        updatedPlayers[color - 1] = player;

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
        updatedPlayers[color-1] = null;

        return Some(this with
        {
            Players = updatedPlayers
        });    
    }
    
    public Option<Game> Move(Player player, Hex hexagon)
    {
        var index = hexagon.Index;
        if (GameState is not GameState.InProgress || player.PlayerNum != CurrentMovePlayerIndex.Value)
            return None;

        var hex = Hexagons.FirstOrDefault(h => h.Index == index);
        if (hex == null || hex.IsTaken)
            return None;

        var updatedHexagons = Hexagons
            .Select(h => h.Index == index ? h.SetPlayer(player.PlayerNum) : h)
            .ToImmutableList();
        
        var newPlayerIndex = CurrentMovePlayerIndex.Increment();
        
        while (Players[newPlayerIndex-1].GaveUp)
        {
            newPlayerIndex = newPlayerIndex.Increment();
        }
        
        var isDraw = IsDraw(updatedHexagons, Players);

        if (isDraw)
        {
            TrainingDataStorage.FlushToDisk(-1);
        }
        
        var won = TrySetWinState(updatedHexagons, player);

        if (won.IsSome)
        {
            Players[player.PlayerNum-1].NumberOfWins++;
            
            TrainingDataStorage.FlushToDisk(Players[player.PlayerNum-1].PlayerNum);

        }
        
        LastChange = DateTime.Now;
        
        
        return Some(this with
        {
            GameState = isDraw || won.IsSome ? GameState.Finished : GameState, 
            Winner = isDraw ? -1 : won.IsSome ? player.PlayerNum : null,
            Hexagons = updatedHexagons,
            CurrentMovePlayerIndex = newPlayerIndex
        });
    }

    private bool IsDraw(IEnumerable<Hex> hexagons, Player[] players)
    {
        return players.All(p =>
        {
            var copied = hexagons.Select(h => new Hex(h.R, h.S, h.Q, h.Index, h.Owner));
            var filledBoard = copied.Select(h => h.IsTaken ? h : h.SetPlayer(p.PlayerNum));
            var res = TrySetWinState(filledBoard, p);
            return !res.IsSome;
        });
    }
    
    
    public Game Reset()
    {
        return this with
        {
            Hexagons = GameHelpers.GenerateInnerHexagonCoordinates(Radius),
            CurrentMovePlayerIndex = new CurrentMovePlayerIndex(1),
            GameState = Players.All(p => !p.GaveUp) ? GameState.InProgress : GameState.Halted,
            Winner = null,
        };
    }

    private Option<Game> TrySetWinState(IEnumerable<Hex> hexagons, Player player)
    {
        System.Collections.Generic.HashSet<Hex> visited = new();
        Queue<Hex> toVisit = new();

        // Add all starting hexes to the queue (e.g., left edge for player 1)
        var startingHexes = hexagons.Where(h => h.Owner == player.PlayerNum && IsStartingEdge(player, h));
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
            foreach (var neighbor in GetNeighbors(hexagons, current))
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

    private bool IsStartingEdge(Player player, Hex hex)
    {
        return player.PlayerNum switch
        {
            1 => hex.Q == -Radius,          // Player 1 starts on the left edge (q == -radius)
            2 => hex.R == -Radius,          // Player 2 starts on the top edge (r == -radius)
            3 => hex.S == -Radius,          // Player 3 starts on the top-right edge (s == -radius)
            _ => throw new ArgumentException("Invalid player number")
        };
    }

    private bool IsOppositeEdge(Player player, Hex hex)
    {
        return player.PlayerNum switch
        {
            1 => hex.Q == Radius,           // Player 1 needs to connect to the right edge (q == radius)
            2 => hex.R == Radius,           // Player 2 needs to connect to the bottom edge (r == radius)
            3 => hex.S == Radius,           // Player 3 needs to connect to the bottom-left edge (s == radius)
            _ => throw new ArgumentException("Invalid player number")
        };
    }

    private IEnumerable<Hex> GetNeighbors(IEnumerable<Hex> hexagons, Hex hex)
    {
        // Define the six possible directions in a hex grid
        var directions = new[]
        {
            (1, -1, 0), (-1, 1, 0), (0, 1, -1), (0, -1, 1), (1, 0, -1), (-1, 0, 1)
        };

        foreach (var (dr, dq, ds) in directions)
        {
            var neighbor = hexagons.FirstOrDefault(h => h.R == hex.R + dr && h.Q == hex.Q + dq && h.S == hex.S + ds);
            if (neighbor != null)
            {
                yield return neighbor;
            }
        }
    }

    public Game Concede(HumanPlayer player)
    {
        var concedingPlayerIndex = Players.ToList().FindIndex(p => p?.PlayerNum == player.PlayerNum);
        if (concedingPlayerIndex == -1)
        {
            throw new InvalidOperationException("Player not found in the game.");
        }

        // Remove the conceding player
        var updatedPlayers = Players.ToArray();
        updatedPlayers[concedingPlayerIndex].GaveUp = true;

        if (updatedPlayers.All(p => p.GaveUp))
        {
            return this with
            {
                GameState = GameState.Finished,
            };        
        }

        var newPlayerIndex = CurrentMovePlayerIndex;
        if (CurrentMovePlayerIndex.Value == concedingPlayerIndex+1)
        {
            while (Players[newPlayerIndex-1].GaveUp)
            {
                newPlayerIndex = CurrentMovePlayerIndex.Increment();
            }
        }

        var remainingPlayers = updatedPlayers.Where(p => !p.GaveUp).ToList();
        if (remainingPlayers.Count == 1)
        {
            var winner = remainingPlayers.First();
            return this with
            {
                Players = new Player[Players.Length],
                Hexagons = GameHelpers.GenerateInnerHexagonCoordinates(Radius),
                CurrentMovePlayerIndex = new CurrentMovePlayerIndex(0),
                GameState = GameState.WaitingForPlayers,
                Winner = winner?.PlayerNum
            };
        }

        return this with
        {
            Players = updatedPlayers,
            CurrentMovePlayerIndex = newPlayerIndex
        };
    }

    public byte[,] To2DArray()
    {
        return To2DArray(Hexagons);
    }

    public short[,] To2DArrayIndices(IReadOnlyList<Hex> updatedHexagons)
    {
        // Calculate array dimensions
        var size = 2 * Radius + 3; // Ensure the board size matches the hexagonal bounds
        var array = new Int16[size, size];

        // Initialize the array with -1 (unplayable spaces)
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                array[i, j] = 255; // Using 255 to represent -1 since byte cannot hold negative values
            }
        }
        // Map axial coordinates to array indices
        foreach (var hex in updatedHexagons)
        {
            var row = hex.R + Radius+1; // Shift R coordinate to positive indices
            var col = hex.Q + Radius+1; // Shift Q coordinate to positive indices

            // Check bounds to ensure no index out of range occurs
            if (row >= 0 && row < size && col >= 0 && col < size)
            {
                array[row, col] = (byte)hex.Index; // Mark playable hexes with 0
            }
            else
            {
                throw new Exception($"Hex out of bounds: Q={hex.Q}, R={hex.R}, Computed Row={row}, Col={col}");
            }
        }
        
        
        // PrintRaw2DArray(array);

        
        return array;
    }

    public byte[,] To2DArray(IReadOnlyList<Hex> updatedHexagons)
    {
        // Calculate array dimensions
        var size = 2 * Radius + 3; // Ensure the board size matches the hexagonal bounds
        var array = new byte[size, size];

        // Initialize the array with -1 (unplayable spaces)
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                array[i, j] = 255; // Using 255 to represent -1 since byte cannot hold negative values
            }
        }
        // Map axial coordinates to array indices
        foreach (var hex in updatedHexagons)
        {
            var row = hex.R + Radius+1; // Shift R coordinate to positive indices
            var col = hex.Q + Radius+1; // Shift Q coordinate to positive indices

            // Check bounds to ensure no index out of range occurs
            if (row >= 0 && row < size && col >= 0 && col < size)
            {
                array[row, col] = (byte)hex.Owner; // Mark playable hexes with 0
            }
            else
            {
                throw new Exception($"Hex out of bounds: Q={hex.Q}, R={hex.R}, Computed Row={row}, Col={col}");
            }
        }
        
        
        // PrintRaw2DArray(array);
        
        return array;
    }

    
    public void PrintRaw2DArray(byte[,] array)
    {
        for (var i = 0; i < array.GetLength(0); i++)
        {
            for (var j = 0; j < array.GetLength(1); j++)
            {
                // Use fixed-width formatting to align numbers
                Console.Write($"{(array[i, j] == 255 ? -1 : array[i, j]),3} ");
            }
            Console.WriteLine(); // New line after each row
        }
        Console.WriteLine();

    }

    public void PrintRaw2DArray(short[,] array)
    {
        for (var i = 0; i < array.GetLength(0); i++)
        {
            for (var j = 0; j < array.GetLength(1); j++)
            {
                // Use fixed-width formatting to align numbers
                Console.Write($"{(array[i, j] == 255 ? -1 : array[i, j]),3} ");
            }
            Console.WriteLine(); // New line after each row
        }
    }
    
}