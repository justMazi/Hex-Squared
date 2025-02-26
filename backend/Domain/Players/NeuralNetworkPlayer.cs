using Domain.Players.MCTS;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Domain.Players;

public class NeuralNetworkPlayer(int playerNum) : AiPlayer(playerNum)
{
    private static readonly InferenceSession Session = new("V:/MFF/bakalarka/Hex-Squared/ai/three_player_hex2.onnx");
    
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        // Step 1: Get the playable hexagons
        var playableIndexes = new HashSet<int>(game.PlayableHexagons.Select(h => h.Index));

        // Step 2: Rotate board for the current player's perspective
        var rotation = game.CurrentMovePlayerIndex.Value switch
        {
            1 => 0,  // No rotation
            2 => 1,  // 60° Clockwise
            3 => 2,  // 120° Clockwise
            _ => throw new Exception("Invalid player")
        };

        var rotatedHexes = MctsHelpers.HexRotation.RotateHexes(game.Hexagons, rotation);

        // Step 3: Convert the rotated hexes into a tensor
        var inputTensor = ConvertHexesToTensor(rotatedHexes, game.Radius);

        // Step 4: Run inference using ONNX model
        var results = Session.Run(new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("board", inputTensor)
        });

        // Step 5: Get policy output (move probabilities)
        var policyOutput = results.First().AsTensor<float>().ToArray();

        // Step 6: Select the best move (highest probability)
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