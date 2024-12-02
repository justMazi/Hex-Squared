using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Application.AI;

public class Hex
{
    public int R { get; set; }
    public int S { get; set; }
    public int Q { get; set; }
    public int Index { get; set; }
    public int Owner { get; set; } // 0 = empty, 1 = player 1, 2 = player 2, etc.

    public Hex(int r, int s, int q, int index, int owner)
    {
        R = r;
        S = s;
        Q = q;
        Index = index;
        Owner = owner;
    }
}

public class Game
{
    public Dictionary<int, Hex> Hexes { get; private set; }
    public int CurrentPlayer { get; private set; }
    private readonly int BoardRadius;

    public Game(IEnumerable<Hex> hexes, int currentPlayer, int radius)
    {
        Hexes = hexes.ToDictionary(h => h.Index);
        CurrentPlayer = currentPlayer;
        BoardRadius = radius;
    }

    public List<int> GetLegalMoves()
    {
        return Hexes.Values
            .Where(h => h.Owner == 0 && Math.Abs(h.Q) != BoardRadius &&
                        Math.Abs(h.S) != BoardRadius && Math.Abs(h.R) != BoardRadius)
            .Select(h => h.Index)
            .ToList();
    }

    public Game ApplyMove(int moveIndex)
    {
        var newHexes = Hexes.Values.Select(h => new Hex(h.R, h.S, h.Q, h.Index, h.Owner)).ToList();
        newHexes.First(h => h.Index == moveIndex).Owner = CurrentPlayer;
        int nextPlayer = CurrentPlayer == 1 ? 2 : 1;
        return new Game(newHexes, nextPlayer, BoardRadius);
    }

    public bool IsTerminal()
    {
        return GetWinner() != null || !GetLegalMoves().Any();
    }

    public int? GetWinner()
    {
        return HasPlayerWon(1) ? 1 : (HasPlayerWon(2) ? 2 : (int?)null);
    }

    private bool HasPlayerWon(int player)
    {
        var visited = new HashSet<int>();
        var toVisit = new Queue<int>();

        foreach (var hex in Hexes.Values.Where(h => h.Owner == player && IsStartingEdge(player, h)))
        {
            toVisit.Enqueue(hex.Index);
            visited.Add(hex.Index);
        }

        while (toVisit.Any())
        {
            var current = toVisit.Dequeue();
            var currentHex = Hexes[current];

            if (IsOppositeEdge(player, currentHex))
                return true;

            foreach (var neighbor in GetNeighbors(currentHex)
                         .Where(h => h.Owner == player && !visited.Contains(h.Index)))
            {
                toVisit.Enqueue(neighbor.Index);
                visited.Add(neighbor.Index);
            }
        }

        return false;
    }

    private bool IsStartingEdge(int player, Hex hex) =>
        player == 1 ? hex.Q == -BoardRadius : hex.R == -BoardRadius;

    private bool IsOppositeEdge(int player, Hex hex) =>
        player == 1 ? hex.Q == BoardRadius : hex.R == BoardRadius;

    public IEnumerable<Hex> GetNeighbors(Hex hex)
    {
        var directions = new[] { (1, -1, 0), (-1, 1, 0), (0, 1, -1), (0, -1, 1), (1, 0, -1), (-1, 0, 1) };

        foreach (var (dr, ds, dq) in directions)
        {
            if (Hexes.TryGetValue(hex.Index + dr + ds + dq, out var neighbor))
                yield return neighbor;
        }
    }

    public Game RotateToPlayer(int player)
    {
        if (player == CurrentPlayer)
            return this;

        var rotatedHexes = Hexes.Values.Select(h => new Hex(-h.S, -h.Q, -h.R, h.Index, h.Owner)).ToList();
        return new Game(rotatedHexes, CurrentPlayer, BoardRadius);
    }

    public Game Clone()
    {
        var clonedHexes = Hexes.Values.Select(h => new Hex(h.R, h.S, h.Q, h.Index, h.Owner)).ToDictionary(h => h.Index);
        return new Game(clonedHexes.Values, CurrentPlayer, BoardRadius);
    }
}

public class Node
{
    public int Move { get; set; }
    public int Wins { get; set; }
    public int Simulations { get; set; }
    public List<Node> Children { get; set; } = new();
    public Node Parent { get; set; }
    public Game GameState { get; set; }

    public Node(int move, Node parent, Game gameState)
    {
        Move = move;
        Parent = parent;
        GameState = gameState;
    }

    public double UCB1(double exploration = 1.414)
    {
        if (Simulations == 0)
            return double.MaxValue;
        return (double)Wins / Simulations + exploration * Math.Sqrt(Math.Log(Parent.Simulations) / Simulations);
    }
}
public static class MCTS
{
    public static int FindBestMove(List<Hex> hexes, int currentPlayer, int iterations = 100000, int radius = 11)
    {
        var rootGame = new Game(hexes, currentPlayer, radius).RotateToPlayer(1);
        var rootNode = new Node(-1, null, rootGame);

        Parallel.For(0, iterations, _ =>
        {
            try
            {
                var node = SelectNode(rootNode);
                int result = Simulate(node.GameState.Clone(), 1);
                Backpropagate(node, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during MCTS iteration: {ex.Message}");
            }
        });

        return rootNode.Children.MaxBy(c => c.Simulations)?.Move ?? -1;
    }

    private static Node SelectNode(Node node)
    {
        while (node.Children.Any())
        {
            lock (node.Children)
            {
                node = node.Children.OrderByDescending(n => n.UCB1()).First();
            }
        }

        if (!node.GameState.IsTerminal())
            ExpandNode(node);

        return node.Children.Any() ? node.Children.First() : node;
    }

    private static void ExpandNode(Node node)
    {
        var legalMoves = node.GameState.GetLegalMoves();

        foreach (var move in legalMoves)
        {
            var childGame = node.GameState.ApplyMove(move);
            var childNode = new Node(move, node, childGame);

            lock (node.Children)
            {
                node.Children.Add(childNode);
            }
        }
    }

    private static int Simulate(Game game, int player)
    {
        var random = new Random();
        while (!game.IsTerminal())
        {
            var legalMoves = game.GetLegalMoves();

            // Use heuristic: Favor middle and path connections
            var scoredMoves = legalMoves
                .Select(move => (Move: move, Score: HeuristicScore(game, move, player)))
                .OrderByDescending(x => x.Score)
                .ToList();

            var bestMove = scoredMoves.First().Move;
            game = game.ApplyMove(bestMove);
        }

        return game.GetWinner() == player ? 1 : 0;
    }

// Heuristic scoring function for prioritizing moves
    private static double HeuristicScore(Game game, int move, int player)
    {
        var hex = game.Hexes[move];
        int boardRadius = game.Hexes.Values.Max(h => Math.Abs(h.R)); // Get board radius dynamically

        // 1. Distance to center (prioritize middle hexes)
        double centerScore = 1.0 / (Math.Abs(hex.R) + Math.Abs(hex.S) + Math.Abs(hex.Q) + 1);

        // 2. Path connections (favor moves that connect to other red hexes)
        int connectionScore = game.GetNeighbors(hex).Count(h => h.Owner == player);

        // 3. Blocking score (optional, deprioritize blocking for now)
        int blockScore = game.GetNeighbors(hex).Count(h => h.Owner != 0 && h.Owner != player);

        // Weight factors for scoring
        double centerWeight = 2.0;       // Middle importance
        double connectionWeight = 3.0;  // Path connection importance
        double blockWeight = 1.0;       // Blocking importance

        return centerWeight * centerScore +
               connectionWeight * connectionScore +
               blockWeight * blockScore;
    }



    private static void Backpropagate(Node node, int result)
    {
        while (node != null)
        {
            node.Simulations++;
            node.Wins += result;
            node = node.Parent;
        }
    }
}

