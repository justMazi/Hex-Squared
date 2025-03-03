using Domain;
using Domain.Players;
using Domain.Players.MCTS;
using Infrastructure.Repositories;



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
    private Random _rand = new Random();
    private Func<Game, int, CancellationToken, Task<int>>[] _heuristics;
    private readonly Dictionary<int, string> _heuristicNames = new()
    {
        { 0, "CenterControlHeuristic" },
        { 1, "EdgeControlHeuristic" },
        { 2, "PathFinderHeuristic" },
        { 3, "RandomPlayer" }
    };

    public HeuristicEvolver()
    {
        _heuristics = new Func<Game, int, CancellationToken, Task<int>>[]
        {
            HeuristicHelper.CenterControlHeuristic,
            HeuristicHelper.EdgeControlHeuristic,
            HeuristicHelper.PathFinderHeuristic,
            HeuristicHelper.RandomPlayer
        };
    }
    
    public void RunEvolution(int popcount = 20, int generations = 40)
    {
        // Update the input size to match board representation
        List<SimpleNn> population = Enumerable.Range(0, popcount)
            .Select(_ => new SimpleNn(675, 32, 4)) // Input: 675, Hidden: 32, Output: 4
            .ToList();

        for (int generation = 0; generation < generations; generation++)
        {
            List<SimpleNn> newPopulation = new();
            Dictionary<int, int> heuristicUsage = new() { { 0, 0 }, { 1, 0 }, { 2, 0 }, { 3, 0 } };
            int totalSelections = 0;

            while (newPopulation.Count < popcount)
            {
                // Select 3 individuals randomly for the tournament
                var candidates = population.OrderBy(_ => _rand.Next()).Take(3).ToList();
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
                Console.WriteLine($"  {_heuristicNames[kvp.Key]}: {kvp.Value} times ({percentage:F2}%)");
            }

            Console.WriteLine();
        }
    }


    private SimpleNn RunTournament(List<SimpleNn> candidates, Dictionary<int, int> heuristicUsage)
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
                return candidates[_rand.Next(3)];
            }
        }
        return candidates[_rand.Next(3)];
    }
}






public class SimpleNn
{
    private readonly double[,] _weightsInputHidden;
    private readonly double[,] _weightsHiddenOutput;
    private readonly double[] _biasHidden;
    private readonly double[] _biasOutput;
    private Random _rand = new Random();

    public SimpleNn(int inputSize, int hiddenSize, int outputSize)
    {
        _weightsInputHidden = RandomMatrix(inputSize, hiddenSize);
        _weightsHiddenOutput = RandomMatrix(hiddenSize, outputSize);
        _biasHidden = RandomArray(hiddenSize);
        _biasOutput = RandomArray(outputSize);
    }

    private double[,] RandomMatrix(int rows, int cols)
    {
        double[,] matrix = new double[rows, cols];
        for (int i = 0; i < rows; i++)
            for (int j = 0; j < cols; j++)
                matrix[i, j] = (_rand.NextDouble() - 0.5) * 0.1;
        return matrix;
    }

    private double[] RandomArray(int size)
    {
        return Enumerable.Range(0, size).Select(_ => (_rand.NextDouble() - 0.5) * 0.1).ToArray();
    }

    public int Predict(double[] input)
    {
        double[] hiddenLayer = MatrixVectorMultiply(_weightsInputHidden, input)
            .Zip(_biasHidden, (x, b) => Math.Max(0, x + b)) // ReLU activation
            .ToArray();

        double[] outputLayer = MatrixVectorMultiply(_weightsHiddenOutput, hiddenLayer)
            .Zip(_biasOutput, (x, b) => x + b)
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

    public SimpleNn Clone()
    {
        var clone = new SimpleNn(_weightsInputHidden.GetLength(0), _weightsInputHidden.GetLength(1), _weightsHiddenOutput.GetLength(1));
        Array.Copy(_weightsInputHidden, clone._weightsInputHidden, _weightsInputHidden.Length);
        Array.Copy(_weightsHiddenOutput, clone._weightsHiddenOutput, _weightsHiddenOutput.Length);
        Array.Copy(_biasHidden, clone._biasHidden, _biasHidden.Length);
        Array.Copy(_biasOutput, clone._biasOutput, _biasOutput.Length);
        return clone;
    }

    public void Mutate(double mutationRate = 0.02)
    {
        MutateMatrix(_weightsInputHidden, mutationRate);
        MutateMatrix(_weightsHiddenOutput, mutationRate);
        MutateArray(_biasHidden, mutationRate);
        MutateArray(_biasOutput, mutationRate);
    }

    private void MutateMatrix(double[,] matrix, double rate)
    {
        for (int i = 0; i < matrix.GetLength(0); i++)
            for (int j = 0; j < matrix.GetLength(1); j++)
                if (_rand.NextDouble() < rate)
                    matrix[i, j] += (_rand.NextDouble() - 0.5) * 0.05;
    }

    private void MutateArray(double[] array, double rate)
    {
        for (int i = 0; i < array.Length; i++)
            if (_rand.NextDouble() < rate)
                array[i] += (_rand.NextDouble() - 0.5) * 0.05;
    }
}







public class EvolvedNeuralNetworkPlayer(int playerNum, SimpleNn nn, Dictionary<int, int> heuristicUsage) : AiPlayer(playerNum)
{
    private readonly SimpleNn _nn = nn;
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
        var rotation = game.CurrentMovePlayerIndex.Value - 1;

        var rotatedHexes = MctsHelpers.HexRotation.RotateHexes(game.Hexagons, rotation);

        var inputArray = ConvertHexesToFlatArray(rotatedHexes, game.Radius);

        int selectedHeuristic = _nn.Predict(inputArray);

        // Track heuristic usage
        if (_heuristicUsage.ContainsKey(selectedHeuristic))
            _heuristicUsage[selectedHeuristic]++;
        else
            _heuristicUsage[selectedHeuristic] = 1;

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


























