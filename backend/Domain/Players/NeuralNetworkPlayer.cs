using Domain.Players.MCTS;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Domain.Players;

public class NeuralNetworkPlayer(int playerNum) : AiPlayer(playerNum)
{
    
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        var session = game.Radius switch
        {
            4 => new InferenceSession(Path.Combine(AppContext.BaseDirectory, "Resources", "small_nn.onnx")),
            6 => new InferenceSession(Path.Combine(AppContext.BaseDirectory, "Resources", "mid_nn.onnx")),
            10 => new InferenceSession(Path.Combine(AppContext.BaseDirectory, "Resources", "big_nn.onnx")),
            _ => throw new ArgumentOutOfRangeException()
        };
        
        
        // Get the playable hexagons
        var playableIndexes = new HashSet<int>(game.PlayableHexagons.Select(h => h.Index));

        var rotation = game.CurrentMovePlayerIndex.Value - 1;


        var rotatedHexes = MctsHelpers.HexRotation.RotateHexes(game.Hexagons, rotation);

        var inputTensor = ConvertHexesToTensor(rotatedHexes, game.Radius);

        // Run inference using ONNX model
        var results = session.Run(new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("board", inputTensor)
        });

        // Get policy output (move probabilities)
        var policyOutput = results.First().AsTensor<float>().ToArray();

        var bestMoveIndex = GetBestMoveIndex(policyOutput, playableIndexes);
        
        return Task.FromResult(bestMoveIndex);
    }

    private DenseTensor<float> ConvertHexesToTensor(IReadOnlyList<Hex> hexes, int radius)
    {
        var size = 2 * radius + 3; // Match board size
        var inputData = new float[1, 3, size, size];
    
        foreach (var hex in hexes)
        {
            var row = hex.R + radius + 1;
            var col = hex.Q + radius + 1;

            if (hex.Owner > 0 && hex.Owner <= 3)
            {
                inputData[0, hex.Owner - 1, row, col] = 1f;
            }
        }

        // Flatten the 4D array into a 1D array
        var flattenedData = inputData.Cast<float>().ToArray();

        return new DenseTensor<float>(flattenedData, new[] { 1, 3, size, size });
    }


    private int GetBestMoveIndex(float[] policyOutput, HashSet<int> validMoves)
    {
        var bestMoveIndex = -1;
        var bestValue = float.NegativeInfinity;

        for (var i = 0; i < policyOutput.Length; i++)
        {
            if (validMoves.Contains(i) && policyOutput[i] > bestValue)
            {
                bestMoveIndex = i;
                bestValue = policyOutput[i];
            }
        }

        if (bestMoveIndex == -1) throw new Exception("No valid move found!");
        return bestMoveIndex;
    }
}