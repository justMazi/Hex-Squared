using System.Diagnostics;
using Domain.Players.MCTS;

namespace MonteCarloConsole;

public struct PlayerNum()
{
    public byte Value { get; private set; } = 0;

    public void Increase()
    {
        Value = (byte)(((Value + 1) % 3) + 1);
    }
}

public class MctsNode
{
    public readonly byte[,] Board = new byte[5,5];
    public int TotalReward = 0;
    public int TotalVisits = 0;
    public readonly List<MctsNode> Children = new();
    public readonly MctsNode? Parent = null;
    public bool IsTerminal { get; set; }
    public PlayerNum PlayerValue { get; set; }

    public MctsNode(byte[,] board, MctsNode parent, PlayerNum playerNum)
    {
        Board = board;
        Parent = parent;
        PlayerValue = playerNum;
        PlayerValue.Increase();
    }
}

class Program
{
    static void Main(string[] args)
    {
        var root = new MctsNode(new byte[5, 5], null!, new PlayerNum());

        var clock = new Stopwatch();
        clock.Start();

        Program program = new Program();
        program.MCTS(root);

        clock.Stop();
        Console.WriteLine($"Elapsed Time: {clock.ElapsedMilliseconds} ms");
    }

    private void MCTS(MctsNode root)
    {
        const int iterations = 1000;
        for (int i = 0; i < iterations; i++)
        {
            // Selection
            var node = Select(root);

            // Expansion
            if (!node.IsTerminal)
                Expand(node);

            // Simulation
            int reward = Simulate(node);

            // Backpropagation
            Backpropagate(node, reward);
        }
    }

    private MctsNode Select(MctsNode node)
    {
        // Implement UCT or another selection policy
        while (node.Children.Count > 0)
        {
            node = BestChild(node);
        }
        return node;
    }

    private void Expand(MctsNode node)
    {
        // Generate child nodes based on possible moves
        List<MctsNode> children = GenerateChildren(node);
        node.Children.AddRange(children);
    }

    private int Simulate(MctsNode node)
    {
        var simulationBoard = (byte[,])node.Board.Clone();
        var currentPlayer = node.PlayerValue;
        
        
        int rows = simulationBoard.GetLength(0);
        int cols = simulationBoard.GetLength(1);

        var untakenPositions = new List<(int, int)>();
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (simulationBoard[i, j] == 0)
                {
                    untakenPositions.Add((i, j));
                }
            }
        }
        
        // Shuffle the list to randomize move order
        var random = new Random();
        for (int i = untakenPositions.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            (untakenPositions[i], untakenPositions[swapIndex]) = (untakenPositions[swapIndex], untakenPositions[i]);
        }
        
        // Alternate players and simulate moves
        foreach (var position in untakenPositions)
        {
            (int row, int col) = position;
            simulationBoard[row, col] = (byte)currentPlayer.Value;
            currentPlayer.Increase();
        }

        return 0;
    }

    private void Backpropagate(MctsNode node, int reward)
    {
        while (node != null)
        {
            node.TotalVisits++;
            node.TotalReward += reward;
            node = node.Parent;
        }
    }

    private MctsNode BestChild(MctsNode node)
    {
        double maxUcb = double.MinValue;
        MctsNode bestChild = default;

        foreach (var child in node.Children)
        {
            double ucb = (child.TotalReward / (double)child.TotalVisits) +
                         Math.Sqrt(2 * Math.Log(node.TotalVisits) / child.TotalVisits);
            if (ucb > maxUcb)
            {
                maxUcb = ucb;
                bestChild = child;
            }
        }

        return bestChild;
    }

    private List<MctsNode> GenerateChildren(MctsNode node)
    {
        var children = new List<MctsNode>();
        
        int rows = node.Board.GetLength(0);
        int cols = node.Board.GetLength(1);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (node.Board[i, j] == 0) // Check if the space is unoccupied
                {
                    var newBoard = (byte[,])node.Board.Clone();
                    newBoard[i, j] = node.PlayerValue.Value;
                    children.Add(new MctsNode(newBoard, node, node.PlayerValue));
                }
            }
        }
        
        
        return children;
    }
}