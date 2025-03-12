namespace Domain.Players.MCTS;

public class MctsNeuralNetworkPlayer_v2(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        var basicMcts = new MctsPlayer(playerNum);
        var neuralMcts = new MctsNeuralNetworkPlayer(playerNum);

        var useNeural = game.PlayableHexagons.Count > game.Radius * 15;

        var res =  useNeural
            ? neuralMcts.CalculateBestMoveAsync(game, cancellationToken)
            : basicMcts.CalculateBestMoveAsync(game, cancellationToken);
        return res;
    }
}