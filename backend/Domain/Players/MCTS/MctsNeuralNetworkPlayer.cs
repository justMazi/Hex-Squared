
using Microsoft.ML.OnnxRuntime;

namespace Domain.Players.MCTS;

public class MctsNeuralNetworkPlayer(int playerNum) : AiPlayer(playerNum)
{
    private static readonly InferenceSession Session = new("V:/MFF/bakalarka/Hex-Squared/ai/three_player_hex2.onnx");

    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        
        throw new NotImplementedException("abcd, toto se musi dodelat");
        var playableIndexes = new HashSet<int>(game.PlayableHexagons.Select(hex => hex.Index));

        var hexes = game.Hexagons.Select(h =>
        {
            if (!playableIndexes.Contains(h.Index) && h.Owner == 0)
            {
                return h.SetPlayer(5);
            }

            return h;
        }).ToList();
        
        var rotation = game.CurrentMovePlayerIndex.Value switch
        {
            1 => 0,  // No rotation needed
            2 => 1,  // 60° Clockwise
            3 => 2,  // 120° Clockwise
            _ => throw new Exception("Invalid player")
        };

        var rotatedHexes = MctsHelpers.HexRotation.RotateHexes(hexes, rotation);
        var indices = game.To2DArrayIndices(rotatedHexes);

        var rotatedHexes2 = MctsHelpers.HexRotation.RotateHexes(rotatedHexes, 1);
        var indices2 = game.To2DArrayIndices(rotatedHexes2);

        var rotatedHexes3 = MctsHelpers.HexRotation.RotateHexes(rotatedHexes2, 1);
        var indices3 = game.To2DArrayIndices(rotatedHexes3);

        var d2Rotate = game.To2DArray(rotatedHexes);
        var root = new MctsNode(d2Rotate, null, (byte)game.CurrentMovePlayerIndex.Value);

        const int iterations = 8_000;
        Mcts(root, iterations, rotation);

        MctsTrainingData.AddTrainingSample(root, game.CurrentMovePlayerIndex.Value, false);
        MctsTrainingData.AddTrainingSample(RemapTrainingData(root, indices2), game.CurrentMovePlayerIndex.Value, true);
        MctsTrainingData.AddTrainingSample(RemapTrainingData(root, indices3), game.CurrentMovePlayerIndex.Value, true);
        
        var final = root.Children.MaxBy(child => child.TotalVisits);
        var (col, row) = final.CoordinatesOfMove.Value;
        
        var index = indices[col, row];
        
        return Task.FromResult((int)index);
    }

    private static MctsNode RemapTrainingData(MctsNode root, short[,] rotationIndices)
    {
        var size = root.Board.GetLength(0);
        var rotatedBoard = new byte[size, size];

        // Remap board state using rotation indices
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                var rotatedIndex = rotationIndices[i, j];
                if (rotatedIndex == 255) continue; // Ignore unplayable spaces

                var originalI = rotatedIndex / size;
                var originalJ = rotatedIndex % size;
                rotatedBoard[i, j] = root.Board[originalI, originalJ];
            }
        }

        var rotatedRoot = new MctsNode(rotatedBoard, null, root.PlayerValue)
        {
            TotalReward = root.TotalReward,
            TotalVisits = root.TotalVisits,
            IsTerminal = root.IsTerminal
        };

        foreach (var child in root.Children)
        {
            var rotatedIndex = rotationIndices[child.CoordinatesOfMove.Value.i, child.CoordinatesOfMove.Value.j];
            if (rotatedIndex == 255) continue;

            var rotatedI = rotatedIndex / size;
            var rotatedJ = rotatedIndex % size;

            rotatedRoot.Children.Add(new MctsNode(rotatedBoard, rotatedRoot, child.PlayerValue, (rotatedI, rotatedJ))
            {
                TotalReward = child.TotalReward,
                TotalVisits = child.TotalVisits,
                IsTerminal = child.IsTerminal
            });
        }

        return rotatedRoot;
    }


    private static MctsNode Mcts(MctsNode root, int iterations, int rotation)
    {
        var whoShouldWin = root.PlayerValue;
        
        Expand(root);
        root.Children.ForEach(n =>
        {
            var reward = Simulate(n, whoShouldWin, rotation);
            Backpropagate(n, reward);
        });
        
        for (var i = 0; i < iterations; i++)
        {
            // Selection
            var node = Select(root);

            // Expansion
            if (!node.IsTerminal)
                Expand(node);

            // Simulation
            var reward = Simulate(node, whoShouldWin, rotation);

            // Backpropagation
            Backpropagate(node, reward);
        }

        return root;
    }

    private static MctsNode Select(MctsNode node)
    {
        while (node.Children.Count > 0)
        {
            node = BestChild(node);
        }
        return node;
    }

    private static void Expand(MctsNode node)
    {
        // Generate child nodes based on possible moves
        var children = GenerateChildren(node);
        node.Children.AddRange(children);
    }

    private static int Simulate(MctsNode node, byte whoShouldWin, int rotation)
    {
        var simulationBoard = (byte[,])node.Board.Clone();
        var currentPlayer = node.PlayerValue;

        var rows = simulationBoard.GetLength(0);
        var cols = simulationBoard.GetLength(1);

        var untakenPositions = new List<(int, int)>();
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                if (simulationBoard[i, j] == 0)
                {
                    untakenPositions.Add((i, j));
                }
            }
        }

        // Shuffle the list to randomize move order
        var random = new Random();
        for (var i = untakenPositions.Count - 1; i > 0; i--)
        {
            var swapIndex = random.Next(i + 1);
            (untakenPositions[i], untakenPositions[swapIndex]) = (untakenPositions[swapIndex], untakenPositions[i]);
        }

        // Alternate players and simulate moves
        foreach (var position in untakenPositions)
        {
            (var row, var col) = position;
            simulationBoard[row, col] = currentPlayer;
            currentPlayer = (byte)((currentPlayer % 3) + 1);
        }

        var pathFinder = new PathFinder();

        
        // Check if any other player has a winning path
        foreach (var player in new[] { 1, 2, 3 })
        {
            if (player != whoShouldWin && pathFinder.HasPath(simulationBoard, player, whoShouldWin-1))
            {
                return -1; // Some other player wins
            }
        }
        
        // Check if the desired player has a winning path
        if (pathFinder.HasPath(simulationBoard, whoShouldWin, whoShouldWin-1))
        {
            return 1; // Desired player wins
        }
        

        // If no one wins, it's a draw
        return 0;
    }
    
    private static void Backpropagate(MctsNode node, int reward)
    {
        while (node != null)
        {
            node.TotalVisits++;
            node.TotalReward += reward;
            node = node.Parent;
        }
    }

    private static MctsNode BestChild(MctsNode node)
    {
        var maxUcb = double.MinValue;
        MctsNode bestChild = null;

        var c = 1.0; // Exploration constant
        double ucb;

        foreach (var child in node.Children)
        {
            if (child.TotalVisits == 0)
            {
                // Assign a very high value to unexplored nodes to prioritize them
                ucb = double.MaxValue;
            }
            else
            {
                // Calculate UCB for explored nodes
                ucb = (child.TotalReward / (double)child.TotalVisits) +
                      c * Math.Sqrt(2 * Math.Log(node.TotalVisits) / child.TotalVisits);
            }

            // Update the best child based on the UCB value
            if (ucb > maxUcb)
            {
                maxUcb = ucb;
                bestChild = child;
            }
        }

        if (bestChild == null)
        {
            throw new InvalidOperationException("No valid child found. This indicates a problem in the tree structure.");
        }

        return bestChild;
    }
        
    private static List<MctsNode> GenerateChildren(MctsNode node)
    {
        var children = new List<MctsNode>();

        var rows = node.Board.GetLength(0);
        var cols = node.Board.GetLength(1);
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                if (node.Board[i, j] == 0) // Check if the space is unoccupied
                {
                    var newBoard = (byte[,])node.Board.Clone();
                    newBoard[i, j] = node.PlayerValue;
                    
                    var newPlayerVal = (byte)((node.PlayerValue % 3) + 1);
                    children.Add(new MctsNode(newBoard, node, newPlayerVal, (i,j)));
                }
            }
        }

        return children;
    }
}
