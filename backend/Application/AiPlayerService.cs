using Application.IRepositories;
using Domain.Players;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Application.AI;
using Microsoft.Extensions.Hosting;

namespace Application;

public class AiPlayerService(IGameRepository gameRepository) : BackgroundService
{
    private readonly TimeSpan _cycleTime = TimeSpan.FromMilliseconds(100);
    private readonly HttpClient _httpClient = new();
    private const string AiServiceUrl = "http://localhost:8000/best-move/";

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
                    if (currentPlayer is AiPlayer aiPlayer)
                    {
                        // Call the AI service to get the best move
                        var bestMoveIndex = -1;

                        if (currentPlayer.PlayerNum == 1)
                        {
                            
                            bestMoveIndex = MCTS.FindBestMove(
                                game.Hexagons.Select(h => new Hex(h.R, h.S, h.Q, h.Index, h.Owner)).ToList(), currentPlayer.PlayerNum);
                        }
                        else
                        {
                            var hexes = game.Hexagons.Where(h => !h.IsTaken && h.Q != 11 && h.R != 11 && h.S != 11).ToList();
                            bestMoveIndex = hexes[Random.Shared.Next(hexes.Count)].Index;
                        }
                        

                        var hexagon = game.Hexagons.First(h => h.Index == bestMoveIndex);
                        var updatedGame = game.Move(currentPlayer, hexagon);
                        updatedGame.IfSome(game1 => gameRepository.SaveGame(game1));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing AI moves.");
                Console.WriteLine(ex.Message);
            }

            await Task.Delay(_cycleTime, stoppingToken); // Run every cycle time
        }

        Console.WriteLine("AIGameBackgroundService stopped.");
    }
    
}
