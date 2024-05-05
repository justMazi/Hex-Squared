using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace HexSquared;


public class WebSocketHub(RunningGamesContainer runningGamesContainer) : Hub
{
    public async Task Move(string message)
    {
        var clickedHex = JsonSerializer.Deserialize<Dto.Hex>(message);
        var game = runningGamesContainer.GetState();
        if (game.TryMove(clickedHex.Player, clickedHex.Index))
        {
            await GetState();
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
                await GetState();
            }
        }    
        Console.WriteLine("=========");

        await GetState();

    }

    private int GetRandomNonReservedIndexNumber(Game game)
    {
        var nonTakenHexagons = game.Hexagons.Where(h => h.Player == 0).ToList();
        if (nonTakenHexagons.Count == 0) return -1;
        var random = new Random().Next(0, nonTakenHexagons.Count);
        return nonTakenHexagons[random].Index;
    }
    
    public record GameState(List<Hex> Hexagons, List<int> FreeColors);
    public async Task GetState()
    {
        // Console.WriteLine("GETSTATE EVENT CALLED");
        var state = runningGamesContainer.GetState();
        var gameState = new GameState(state.Hexagons, state.NonReservedColors);
        var game = JsonSerializer.Serialize(gameState);
        await Clients.All.SendAsync("GetState", game);
    }
    
    public async Task SelectColor(int number)
    {
        // Console.WriteLine("SelectColor EVENT CALLED");
        var game = runningGamesContainer.GetState();
        game.ReserveColor(number, false);
        await GetState();
    }    
    
    public async Task FillWithAiPlayers()
    {
        var game = runningGamesContainer.GetState();
        foreach (var colorNum in game.NonReservedColors.ToArray())
        {
            game.ReserveColor(colorNum, true);
        }
        await GetState();
        
        // Console.WriteLine("FillWithAiPlayers EVENT CALLED");
        await RunAiMoves(game);
    }
}



