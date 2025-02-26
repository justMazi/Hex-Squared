namespace Domain.Players.MCTS;

public class TrainingSample
{
    public readonly int CurrentPlayer;
    public readonly bool IsRotated;
    public List<int> Player1Board { get; set; }
    public List<int> Player2Board { get; set; }
    public List<int> Player3Board { get; set; }
    public float RootValue { get; set; }
    public float[] MoveProbabilities { get; set; }

    public TrainingSample(byte[] p1, byte[] p2, byte[] p3, float[] moveProbs, int currentPlayer , bool isRotated)
    {
        CurrentPlayer = currentPlayer;
        IsRotated = isRotated;
        Player1Board = ConvertToIntList(p1);
        Player2Board = ConvertToIntList(p2);
        Player3Board = ConvertToIntList(p3);
        MoveProbabilities = moveProbs;
    }

    // Convert byte array to a list of integers for JSON serialization
    private static List<int> ConvertToIntList(byte[] array)
    {
        return array.Select(b => (int)b).ToList();
    }
}
