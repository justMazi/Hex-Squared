using Application.IRepositories;
using Domain.Players;
using Microsoft.Extensions.Hosting;

namespace Application;

public class AiPlayerService(IGameRepository gameRepository) : BackgroundService
{
    private readonly TimeSpan _cycleTime = TimeSpan.FromMilliseconds(100);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("AIGameBackgroundService started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                foreach (var game in gameRepository.GetAllInProgressGamesWithAi())
                {
                    var currentPlayer = game.Players[game.CurrentMovePlayerIndex - 1];
                    
                    if (currentPlayer is not AiPlayer player) continue;
                    
                    var bestMoveIndex = await player.CalculateBestMoveAsync(game, cancellationToken);

                    var hexagon = game.Hexagons.First(h => h.Index == bestMoveIndex);
                    var updatedGame = game.Move(player, hexagon);
                    updatedGame.IfSome(game1 => gameRepository.SaveGame(game1));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing AI moves.");
                Console.WriteLine(ex.Message);
            }

            await Task.Delay(_cycleTime, cancellationToken);
        }

        Console.WriteLine("AIGameBackgroundService stopped.");
    }
}
