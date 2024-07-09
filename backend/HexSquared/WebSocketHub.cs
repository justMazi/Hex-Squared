using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace HexSquared;


public class WebSocketHub(RunningGamesContainer runningGamesContainer) : Hub
{
    public async Task Move(string message, string gameCode)
    {
        var clickedHex = JsonSerializer.Deserialize<Dto.Hex>(message);
        var game = runningGamesContainer.GetState(gameCode);
        if (game.TryMove(clickedHex.Player, clickedHex.Index))
        {
            await GetState(game.GameCode);
        }
        Console.WriteLine("MOVE EVENT CALLED");

        await RunAiMoves(game);
    }

    private async Task RunAiMoves(Game game)
    {
        while (game.IsCurrentMoveArtificial())
        {
            // here will be plugged in async fetch from a model that will decide the next move
            var index = GetRandomNonReservedIndexNumber(game);
            // Console.WriteLine(index);
            if (index == -1) return;
            if (game.TryMove(game._currentMovePlayerIndex, index))
            {
                Console.WriteLine($"ai move to index {index}");
                await GetState(game.GameCode);
            }
        }    
        Console.WriteLine("=========");

        await GetState(game.GameCode);

    }

    private int GetRandomNonReservedIndexNumber(Game game)
    {
        var nonTakenHexagons = game.Hexagons.Where(h => h.Player == 0).ToList();
        if (nonTakenHexagons.Count == 0) return -1;
        var random = new Random().Next(0, nonTakenHexagons.Count);
        return nonTakenHexagons[random].Index;
    }
    
    public record GameState(List<Hex> Hexagons, List<int> FreeColors, string GameCode);
    public async Task GetState(string gameName)
    {
        // Console.WriteLine("GETSTATE EVENT CALLED");
        var state = runningGamesContainer.GetState(gameName);
        var gameState = new GameState(state.Hexagons, state.NonReservedColors, state.GameCode);
        var game = JsonSerializer.Serialize(gameState);
        await Clients.All.SendAsync("GetState", game);
    }
    
    public async Task SelectColor(int number, string gameName)
    {
        // Console.WriteLine("SelectColor EVENT CALLED");
        var game = runningGamesContainer.GetState(gameName);
        game.ReserveColor(number, false);
        await GetState(gameName);
    }    
    
    public async Task FillWithAiPlayers(string gameName)
    {
        var game = runningGamesContainer.GetState(gameName);
        foreach (var colorNum in game.NonReservedColors.ToArray())
        {
            game.ReserveColor(colorNum, true);
        }
        await GetState(gameName);
        
        // Console.WriteLine("FillWithAiPlayers EVENT CALLED");
        await RunAiMoves(game);
    }
}



