using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace HexSquared;

public record GameState(List<Hex> Hexagons, List<int> FreeColors);

public class WebsocketHub(RunningGamesContainer runningGamesContainer) : Hub
{
    public async Task Move(string message)
    {
        Console.WriteLine("MOVE EVENT CALLED");
        var clickedHex = JsonSerializer.Deserialize<Dto.Hex>(message);
        var find = runningGamesContainer.GetState().hexagons.Find(h => h.Index == clickedHex.Index) ?? throw new ArgumentException();
        find.SetPlayer(clickedHex.Player);
        await GetState();
    }
    
    public async Task GetState()
    {
        Console.WriteLine("GETSTATE EVENT CALLED");
        var state = runningGamesContainer.GetState();
        var gameState = new GameState(state.hexagons, state.freeColors);
        var game = JsonSerializer.Serialize(gameState);
        await Clients.All.SendAsync("GetState", game);
    }
    
    public async Task SelectColor(int number)
    {
        Console.WriteLine("SelectColor EVENT CALLED");
        var game = runningGamesContainer.GetState();
        game.freeColors.Remove(number);
        await GetState();
        
    }
}



