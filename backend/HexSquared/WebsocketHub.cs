using System.Text.Json;
using Microsoft.AspNetCore.SignalR;

namespace HexSquared;

public class WebsocketHub(RunningGamesContainer runningGamesContainer) : Hub
{
    public async Task Move(string message)
    {
        var clickedHex = JsonSerializer.Deserialize<Dto.Hex>(message);
        var find = runningGamesContainer.GetState().hexagons.Find(h => h.Index == clickedHex.Index) ?? throw new ArgumentException();
        find.SetPlayer(clickedHex.Player);
        await GetState();
    }
    
    public async Task GetState()
    {
        var jsonState = JsonSerializer.Serialize(runningGamesContainer.GetState().hexagons);
        await Clients.All.SendAsync("GetState", jsonState);
    }
}



