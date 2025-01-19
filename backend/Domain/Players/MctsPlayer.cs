using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Players;

public class MctsPlayer(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        return Task.FromResult(game.PlayableHexagons[Random.Shared.Next(game.PlayableHexagons.Count)].Index);
    }
}
