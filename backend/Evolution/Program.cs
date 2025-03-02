using Domain;
using Domain.Players;
using Domain.Players.MCTS;
using Infrastructure.Repositories;

public class SimpleNN
{
    public double[,] weightsInputHidden;
    public double[,] weightsHiddenOutput;
    public double[] biasHidden;
    public double[] biasOutput;
    private Random rand = new Random();

    public SimpleNN(int inputSize, int hiddenSize, int outputSize)
    {
        weightsInputHidden = RandomMatrix(inputSize, hiddenSize);
        weightsHiddenOutput = RandomMatrix(hiddenSize, outputSize);
        biasHidden = RandomArray(hiddenSize);
        biasOutput = RandomArray(outputSize);
    }

    private double[,] RandomMatrix(int rows, int cols)
    {
        double[,] matrix = new double[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                matrix[i, j] = (rand.NextDouble() - 0.5) * 0.1;
        return matrix;
    }

    private double[] RandomArray(int size)
    {
        return Enumerable.Range(0, size).Select(_ => (rand.NextDouble() - 0.5) * 0.1).ToArray();
    }

    public int Predict(double[] input)
    {
        double[] hiddenLayer = MatrixVectorMultiply(weightsInputHidden, input)
            .Zip(biasHidden, (x, b) => Math.Max(0, x + b)) // ReLU activation
            .ToArray();

        double[] outputLayer = MatrixVectorMultiply(weightsHiddenOutput, hiddenLayer)
            .Zip(biasOutput, (x, b) => x + b)
            .ToArray();

        return ArgMax(Softmax(outputLayer));
    }

    private double[] MatrixVectorMultiply(double[,] matrix, double[] vector)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        double[] result = new double[cols];

        for (int j = 0; j < cols; j++)
        {
            result[j] = 0;
            for (int i = 0; i < rows; i++)
                result[j] += matrix[i, j] * vector[i];
        }
        return result;
    }

    private double[] Softmax(double[] input)
    {
        double max = input.Max();
        double[] expValues = input.Select(x => Math.Exp(x - max)).ToArray();
        double sum = expValues.Sum();
        return expValues.Select(x => x / sum).ToArray();
    }

    private int ArgMax(double[] array)
    {
        return Array.IndexOf(array, array.Max());
    }

    public SimpleNN Clone()
    {
        var clone = new SimpleNN(weightsInputHidden.GetLength(0), weightsInputHidden.GetLength(1), weightsHiddenOutput.GetLength(1));
        Array.Copy(weightsInputHidden, clone.weightsInputHidden, weightsInputHidden.Length);
        Array.Copy(weightsHiddenOutput, clone.weightsHiddenOutput, weightsHiddenOutput.Length);
        Array.Copy(biasHidden, clone.biasHidden, biasHidden.Length);
        Array.Copy(biasOutput, clone.biasOutput, biasOutput.Length);
        return clone;
    }

    public void Mutate(double mutationRate = 0.02)
    {
        MutateMatrix(weightsInputHidden, mutationRate);
        MutateMatrix(weightsHiddenOutput, mutationRate);
        MutateArray(biasHidden, mutationRate);
        MutateArray(biasOutput, mutationRate);
    }

    private void MutateMatrix(double[,] matrix, double rate)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
            for (int j = 0; j < matrix.GetLength(1); j++)
                if (rand.NextDouble() < rate)
                    matrix[i, j] += (rand.NextDouble() - 0.5) * 0.05;
    }

    private void MutateArray(double[] array, double rate)
    {
        for (int i = 0; i < array.Length; i++)
            if (rand.NextDouble() < rate)
                array[i] += (rand.NextDouble() - 0.5) * 0.05;
    }
}







public class EvolvedNeuralNetworkPlayer(int playerNum, SimpleNN nn, Dictionary<int, int> heuristicUsage) : AiPlayer(playerNum)
{
    private readonly SimpleNN _nn = nn;
    private readonly Dictionary<int, int> _heuristicUsage = heuristicUsage;

    private readonly Func<Game, int, CancellationToken, Task<int>>[] _heuristics =
    {
        HeuristicHelper.CenterControlHeuristic,
        HeuristicHelper.EdgeControlHeuristic,
        HeuristicHelper.PathFinderHeuristic,
        HeuristicHelper.RandomPlayer
    };

    public override async Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        // Step 1: Rotate the board for the player's perspective
        var rotation = game.CurrentMovePlayerIndex.Value switch
        {
            1 => 0,  // No rotation
            2 => 1,  // 60° Clockwise
            3 => 2,  // 120° Clockwise
            _ => throw new Exception("Invalid player")
        };

        var rotatedHexes = MctsHelpers.HexRotation.RotateHexes(game.Hexagons, rotation);

        // Step 2: Extract a flat array representation of the board
        var inputArray = ConvertHexesToFlatArray(rotatedHexes, game.Radius);

        // Step 3: Use the neural network to select the best heuristic
        int selectedHeuristic = _nn.Predict(inputArray);

        // Step 4: Track heuristic usage
        if (_heuristicUsage.ContainsKey(selectedHeuristic))
            _heuristicUsage[selectedHeuristic]++;
        else
            _heuristicUsage[selectedHeuristic] = 1;

        // Step 5: Call the selected heuristic and return the move
        return await _heuristics[selectedHeuristic](game, playerNum, cancellationToken);
    }

    private double[] ConvertHexesToFlatArray(IReadOnlyList<Hex> hexes, int radius)
    {
        var size = 2 * radius + 3; // Match board size
        var inputData = new double[3, size, size]; // 3 channels for the 3 players

        foreach (var hex in hexes)
        {
            var row = hex.R + radius + 1;
            var col = hex.Q + radius + 1;

            if (hex.Owner > 0 && hex.Owner <= 3)
            {
                inputData[hex.Owner - 1, row, col] = 1f;
            }
        }

        // Flatten the 3D array into a 1D array for neural network input
        return inputData.Cast<double>().ToArray();
    }
}



























class Program
{
    static void Main()
    {
        var evolver = new HeuristicEvolver();
        evolver.RunEvolution();
    }
}





public class HeuristicEvolver
{
    private Random rand = new Random();
    private Func<Game, int, CancellationToken, Task<int>>[] heuristics;
    private readonly Dictionary<int, string> heuristicNames = new()
    {
        { 0, "CenterControlHeuristic" },
        { 1, "EdgeControlHeuristic" },
        { 2, "PathFinderHeuristic" },
        { 3, "RandomPlayer" }
    };

    public HeuristicEvolver()
    {
        heuristics = new Func<Game, int, CancellationToken, Task<int>>[]
        {
            HeuristicHelper.CenterControlHeuristic,
            HeuristicHelper.EdgeControlHeuristic,
            HeuristicHelper.PathFinderHeuristic,
            HeuristicHelper.RandomPlayer
        };
    }
    
    public void RunEvolution()
    {
        const int popcount = 20;
        const int generations = 40;

        // Update the input size to match board representation
        List<SimpleNN> population = Enumerable.Range(0, popcount)
            .Select(_ => new SimpleNN(675, 32, 4)) // Input: 675, Hidden: 32, Output: 4
            .ToList();

        for (int generation = 0; generation < generations; generation++)
        {
            List<SimpleNN> newPopulation = new();
            Dictionary<int, int> heuristicUsage = new() { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } };
            int totalSelections = 0;

            while (newPopulation.Count < popcount)
            {
                // Select 3 individuals randomly for the tournament
                var candidates = population.OrderBy(_ => rand.Next()).Take(3).ToList();
                var winner = RunTournament(candidates, heuristicUsage);
                newPopulation.Add(winner);
            }

            // Mutate new population
            foreach (var individual in newPopulation)
            {
                individual.Mutate();
            }

            population = newPopulation;

            // Print heuristic selection stats
            Console.WriteLine($"Generation {generation} evolved.");
            Console.WriteLine($"Heuristic Selection Stats:");

            totalSelections = heuristicUsage.Values.Sum();
            foreach (var kvp in heuristicUsage)
            {
                double percentage = totalSelections > 0 ? (double)kvp.Value / totalSelections * 100 : 0;
                Console.WriteLine($"  {heuristicNames[kvp.Key]}: {kvp.Value} times ({percentage:F2}%)");
            }

            Console.WriteLine();
        }
    }


    private SimpleNN RunTournament(List<SimpleNN> candidates, Dictionary<int, int> heuristicUsage)
    {
        foreach (var gameNum in Enumerable.Range(0, 10)) // Each tournament plays 1 game
        {
            var gameRepository = new GameRepository();
            var cancellationToken = new CancellationToken();

            var game = gameRepository.CreateNewGame(new GameId(gameNum.ToString()), 6, typeof(MctsPlayer));

            var players = candidates.Select((nn, index) => new EvolvedNeuralNetworkPlayer(index + 1, nn, heuristicUsage)).ToList();

            foreach (var player in players)
            {
                game = game.PickColor(player, player.PlayerNum).Match(
                    Some: updatedGame =>
                    {
                        gameRepository.SaveGame(updatedGame);
                        return updatedGame;
                    },
                    None: () => throw new Exception("Couldn't pick the color")
                );
            }

            while (game.GameState != GameState.Finished)
            {
                var currentPlayer = game.Players[game.CurrentMovePlayerIndex - 1];

                if (currentPlayer is not AiPlayer player)
                    throw new Exception($"Experiments allow only {nameof(AiPlayer)} players");

                var bestMoveIndex = player.CalculateBestMoveAsync(game, cancellationToken).GetAwaiter().GetResult();
                var hexagon = game.Hexagons.FirstOrDefault(h => h.Index == bestMoveIndex);

                if (hexagon == null)
                    throw new Exception($"Invalid move index {bestMoveIndex} for player {currentPlayer.PlayerNum}");

                game = game.Move(player, hexagon).Match(
                    Some: updatedGame =>
                    {
                        gameRepository.SaveGame(updatedGame);
                        return updatedGame;
                    },
                    None: () => throw new Exception("Failed to make a move")
                );
            }

            // Determine winner
            var maxWins = game.Players.Max(p => p.NumberOfWins);
            var winningPlayers = game.Players.Where(p => p.NumberOfWins == maxWins).ToList();

            if (winningPlayers.Count == 1)
            {
                return candidates[winningPlayers[0].PlayerNum - 1];
            }
            else
            {
                return candidates[rand.Next(3)];
            }
        }
        return candidates[rand.Next(3)];
    }
}
