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
                    if (currentPlayer is AiPlayer)
                    {
                        var bestMoveIndex = -1;

                        if (currentPlayer.PlayerNum == 1)
                        {
                            // Prepare request data
                            var requestData = new
                            {
                                board = game.Hexagons.Select(h => new 
                                {
                                    R = h.R,
                                    S = h.S,
                                    Q = h.Q,
                                    Index = h.Index,
                                    Owner = h.Owner
                                }).ToList(),
                                player = currentPlayer.PlayerNum,
                                iter_limit = 10, // Adjust as needed
                                num_threads = 4   // Adjust as needed
                            };

                            // Serialize request to JSON
                            var requestJson = JsonSerializer.Serialize(requestData);

                            // Make HTTP POST request to AI service
                            var response = await _httpClient.PostAsync(AiServiceUrl, new StringContent(requestJson, Encoding.UTF8, "application/json"), stoppingToken);

                            if (response.IsSuccessStatusCode)
                            {
                                // Parse response to get the best move index
                                var responseContent = await response.Content.ReadAsStringAsync(stoppingToken);
                                var responseJson = JsonDocument.Parse(responseContent);
                                bestMoveIndex = responseJson.RootElement.GetProperty("BestMove").GetInt32();
                            }
                            else
                            {
                                Console.WriteLine($"AI service returned error: {response.StatusCode}");
                                continue;
                            }
                        }
                        else
                        {
                            var hexes = game.Hexagons.Where(h => !h.IsTaken && h.Q != 11 && h.R != 11 && h.S != 11).ToList();
                            bestMoveIndex = hexes[Random.Shared.Next(hexes.Count)].Index;
                        }

                        // Perform the move and save the game state
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
