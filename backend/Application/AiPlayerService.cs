using Application.IRepositories;
using Domain.Players;
using Microsoft.Extensions.Hosting;

namespace Application;

public class AiPlayerService(IGameRepository gameRepository) : BackgroundService
{
    private readonly TimeSpan _cycleTime = TimeSpan.FromMilliseconds(100);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("AIGameBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var game in gameRepository.GetAllInProgressGamesWithAi())
                {
                    var currentPlayer = game.Players[game.CurrentMovePlayerIndex - 1];
                    if (currentPlayer is AiPlayer)
                    {
                        var updatedGame = game.Move(currentPlayer, game.Hexagons.FirstOrDefault(h => h.IsTaken is false && h.S != 11 && h.Q != 11 && h.R != 11)!);
                        updatedGame.IfSome(gameRepository.SaveGame);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing AI moves.");
                Console.WriteLine(ex.Message);
            }

            await Task.Delay(_cycleTime, stoppingToken); // Run every 1 second
        }

        Console.WriteLine("AIGameBackgroundService stopped.");
    }
}