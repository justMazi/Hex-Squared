using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Domain.Players.MCTS;

public class MctsNeuralNetworkPlayer(int playerNum) : AiPlayer(playerNum)
{
    private static InferenceSession Session;

    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        
        Session = game.Radius switch
        {
            4 => new InferenceSession(Path.Combine(AppContext.BaseDirectory, "Resources", "small_nn.onnx")),
            6 => new InferenceSession(Path.Combine(AppContext.BaseDirectory, "Resources", "mid_nn.onnx")),
            10 => new InferenceSession(Path.Combine(AppContext.BaseDirectory, "Resources", "big_nn.onnx")),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        var playableIndexes = new HashSet<int>(game.PlayableHexagons.Select(hex => hex.Index));
        
        var hexes = game.Hexagons.Select(h =>
        {
            if (!playableIndexes.Contains(h.Index) && h.Owner == 0)
            {
                return h.SetPlayer(5);
            }
            return h;
        }).ToList();

        var rotation = game.CurrentMovePlayerIndex.Value - 1;

        var rotatedHexes = MctsHelpers.HexRotation.RotateHexes(hexes, rotation);
        var indices = game.To2DArrayIndices(rotatedHexes);
        
        var d2Rotate = game.To2DArray(rotatedHexes);
        var root = new MctsNode(d2Rotate, null, (byte)game.CurrentMovePlayerIndex.Value);

        const int iterations = 250;
        Mcts(root, iterations);

        var final = root.Children.MaxBy(child => child.TotalVisits);
        var (col, row) = final.CoordinatesOfMove.Value;
        var index = indices[col, row];
        
        return Task.FromResult((int)index);
    }

    private static MctsNode Mcts(MctsNode root, int iterations)
    {
        Expand(root);
        root.Children.ForEach(n =>
        {
            var reward = EvaluateWithNeuralNetwork(n);
            Backpropagate(n, reward);
        });
        
        for (var i = 0; i < iterations; i++)
        {
            var node = Select(root);
            if (!node.IsTerminal)
                Expand(node);

            var reward = EvaluateWithNeuralNetwork(node);
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
        node.Children.AddRange(GenerateChildren(node));
    }

    private static float EvaluateWithNeuralNetwork(MctsNode node)
    {
        var boardTensor = ConvertBoardToTensor(node.Board);
        var results = Session.Run(new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("board", boardTensor)
        });

        return results.Last().AsTensor<float>().First();
    }

    private static float[,] EvaluatePolicyForNode(MctsNode node)
    {
        var boardTensor = ConvertBoardToTensor(node.Board);
        var results = Session.Run(new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("board", boardTensor)
        });

        var policyOutput = results.First().AsTensor<float>().ToArray();
        var size = node.Board.GetLength(0);
        var policyMatrix = new float[size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                policyMatrix[i, j] = policyOutput[i * size + j];
            }
        }

        return policyMatrix;
    }

    private static void Backpropagate(MctsNode node, float reward)
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
        var maxScore = double.MinValue;
        MctsNode bestChild = null;
        
        var c = 1.0; 
        var policyOutput = EvaluatePolicyForNode(node);

        foreach (var child in node.Children)
        {
            double ucb = child.TotalVisits == 0
                ? double.MaxValue
                : (child.TotalReward / (double)child.TotalVisits) +
                  c * Math.Sqrt(2 * Math.Log(node.TotalVisits) / child.TotalVisits);

            var policyScore = policyOutput[child.CoordinatesOfMove.Value.i, child.CoordinatesOfMove.Value.j];
            var score = 0.3 * policyScore + 0.7 * ucb;

            if (score > maxScore)
            {
                maxScore = score;
                bestChild = child;
            }
        }

        return bestChild ?? throw new InvalidOperationException("No valid child found.");
    }
    
    private static List<MctsNode> GenerateChildren(MctsNode node)
    {
        var children = new List<MctsNode>();
        var size = node.Board.GetLength(0);
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                if (node.Board[i, j] == 0)
                {
                    var newBoard = (byte[,])node.Board.Clone();
                    newBoard[i, j] = node.PlayerValue;
                    
                    var newPlayerVal = (byte)((node.PlayerValue % 3) + 1);
                    children.Add(new MctsNode(newBoard, node, newPlayerVal, (i, j)));
                }
            }
        }
        return children;
    }

    private static DenseTensor<float> ConvertBoardToTensor(byte[,] board)
    {
        var size = board.GetLength(0);
        var tensorData = new float[1, 3, size, size];

        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                if (board[i, j] > 0 && board[i, j] <= 3)
                {
                    tensorData[0, board[i, j] - 1, i, j] = 1f;
                }
            }
        }

        var flattenedData = tensorData.Cast<float>().ToArray();
        return new DenseTensor<float>(flattenedData, new[] { 1, 3, size, size });
    }
}
