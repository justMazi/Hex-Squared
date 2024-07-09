namespace HexSquared;

public class Game(string gameCode)
{
    public IPlayer[] Players = new IPlayer[3];
    public readonly List<Hex> Hexagons = GameHelpers.GenerateInnerHexagonCoordinates();
    public readonly List<int> NonReservedColors = [1,2,3];
    public CurrentMovePlayerIndex _currentMovePlayerIndex = new(1);
    public State State = State.PlayerGather;
    public readonly string GameCode = gameCode;

    public bool TryMove(int player, int index)
    {
        // Game is not even running
        if (State is not State.InProgress)
        {
            return false;
        }
        
        // its not the players turn
        if (player != _currentMovePlayerIndex)
        {
            return false;
        }
        
        var hex = Hexagons.Find(h => h.Index == index);
        
        // the clicked hex field is already owned by someone else
        if (hex.Player != 0)
        {
            return false;
        }
        
        hex.SetPlayer(player);
        _currentMovePlayerIndex.Increment();
        return true;
    }
    
    public void ReserveColor(int number, bool isAiPlayer)
    {
        NonReservedColors.Remove(number);
        Players[number-1] = isAiPlayer ? new AIPlayer() : new HumanPlayer();
        if (NonReservedColors.Count is 0)
        {
            State = State.InProgress;
        }    
    }

    public bool IsCurrentMoveArtificial()
    {
        return (Players[_currentMovePlayerIndex - 1] as AIPlayer) is not null;
    }
}


