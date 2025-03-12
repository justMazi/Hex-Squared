using Application.IRepositories;
using Microsoft.Extensions.Hosting;

namespace Application;

public class GameCleanupService(IGameRepository gameRepository) : BackgroundService
{
    private readonly TimeSpan _cycleTime = TimeSpan.FromMilliseconds(100);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("AIGameBackgroundService started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var gamesToDelete = gameRepository.GetAllGames()
                    .Where(g => g != null && g.Players.All(p => p is not null) && (g.Players.All(p => p.GaveUp) || g.LastChange.AddDays(1) < DateTime.Now)).ToList();
                foreach (var game in gamesToDelete)
                {
                    Console.WriteLine($"Game deleted with id {game.Id}");
                    gameRepository.DeleteGame(game);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing cleanup.");
                Console.WriteLine(ex.Message);
            }

            await Task.Delay(_cycleTime, cancellationToken);
        }

        Console.WriteLine("AIGameBackgroundService stopped.");
    }
}