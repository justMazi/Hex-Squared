namespace Domain.Players.MCTS;

public class MctsNode
{
    public readonly byte[,] Board = new byte[7, 7];
    public float TotalReward = 0;
    public int TotalVisits = 0;
    public readonly List<MctsNode> Children = new();
    public readonly MctsNode? Parent = null;
    public readonly (int i, int j)? CoordinatesOfMove;
    public bool IsTerminal { get; set; }
    public byte PlayerValue { get; set; }
    public MctsNode(byte[,] board, MctsNode? parent, byte playerNum, (int i, int j)? coordinatesOfMove = null)
    {
        Board = board;
        Parent = parent;
        PlayerValue = playerNum;
        CoordinatesOfMove = coordinatesOfMove;
    }
}