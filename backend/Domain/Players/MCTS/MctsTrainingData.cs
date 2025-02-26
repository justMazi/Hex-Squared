namespace Domain.Players.MCTS;



public class MctsTrainingData
{
    public static void AddTrainingSample(MctsNode root, int currentPlayer, bool isRotated)
    {
        var sample = GenerateTrainingData(root, currentPlayer, isRotated);
        TrainingDataStorage.AddTrainingSample(sample);
    }
    public static TrainingSample GenerateTrainingData(MctsNode root, int currentPlayer, bool isRotated)
    {
        var size = root.Board.GetLength(0);
        var totalCells = size * size;

        // Initialize binary 1D arrays for each player
        var player1Board = new byte[totalCells];
        var player2Board = new byte[totalCells];
        var player3Board = new byte[totalCells];

        // Fill the binary boards directly in 1D format
        for (var i = 0; i < size; i++)
        {
            for (var j = 0; j < size; j++)
            {
                var index = i * size + j;
                if (root.Board[i, j] == 1) player1Board[index] = 1;
                if (root.Board[i, j] == 2) player2Board[index] = 1;
                if (root.Board[i, j] == 3) player3Board[index] = 1;
            }
        }
        

        // Initialize move probabilities
        var moveProbabilities = new float[totalCells];
        var totalVisits = root.Children.Sum(child => child.TotalVisits);

        foreach (var child in root.Children)
        {
            var index = child.CoordinatesOfMove.Value.i * size + child.CoordinatesOfMove.Value.j;
            moveProbabilities[index] = totalVisits > 0 ? (float)child.TotalVisits / totalVisits : 0f;
        }

        return new TrainingSample(player1Board, player2Board, player3Board, moveProbabilities, currentPlayer, isRotated);
    }

}
