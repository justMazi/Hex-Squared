using System.Text;
using System.Text.Json;
using Application.IRepositories;
using Application.Services.Interfaces;
using Domain;
using Domain.Players;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace HexSquared.Api.v1;

[ApiController]
[Route("api/v1/")]
public class GameController(IGameService gameService, IGameRepository gameRepository) : ControllerBase
{
    private IGameService GameService { get; } = gameService;
    private IGameRepository GameRepository { get; } = gameRepository;

    [HttpGet("game/{id}")]
    public IActionResult GetGameState(string id)
    {
        var gameId = new GameId(id);
        var game = GameService.GetOrCreate(gameId);
        return Ok(game);
    }

    [HttpPost("game/{id}/pickColor")]
    public IActionResult PickColor(string id, [FromQuery] int color)
    {
        HttpContext.Request.Cookies.TryGetValue("hex_session", out var session);
        if (session != null)
        {
            var jsonData = Encoding.UTF8.GetString(Convert.FromBase64String(session));
            var sessionData = JsonSerializer.Deserialize<SessionCookieData>(jsonData);
                
            if (sessionData?.Id == id)
            {
                BadRequest("You have already picked a color");
            }
            else
            {
                GameService.Get(new GameId(id)).Match(
                    // caller has a cookie referencing some other game, that may or may not exist
                    existingGame =>
                    {
                        if (existingGame.GameState == GameState.InProgress)
                        {
                            Redirect($"/{existingGame.Id}");
                        }
                        else if (existingGame.GameState == GameState.Finished)
                        {
                            GameRepository.DeleteGame(existingGame);
                        }
                        else if (existingGame.GameState == GameState.WaitingForPlayers)
                        {
                            existingGame.UnpickColor(color);
                        }
                    },
                    () => { HttpContext.Response.Cookies.Delete("hex_session");}
                );
            }
        }

        var gameId = new GameId(id);
        var game = GameService.GetOrCreate(gameId).PickColor(new HumanPlayer(color), color);
        
        return game.Match<IActionResult>(
            Some: updated =>
            {
                GameRepository.SaveGame(updated);
                int index = Array.FindIndex(updated.Players, item => item != null);
                var sessionData = new SessionCookieData(updated.Id.ToString(), index);

                var jsonData = JsonSerializer.Serialize(sessionData);
                var base64Data = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonData));
                HttpContext.Response.Cookies.Append("hex_session", base64Data);

                return Ok(updated);
            },
            None: () => BadRequest("Color already taken.")
        );
    }
    
    [HttpPost("game/{id}/move")]
    public IActionResult Move(string id, [FromQuery] int index)
    {
        var gameId = new GameId(id);
        var game = GameService.GetOrCreate(gameId);

        HttpContext.Request.Cookies.TryGetValue("hex_session", out var session);

        var moveResult = ExtractSession(session).BiBind(
            data => game.Move(new HumanPlayer(data.PlayerNumber), index),
            () => Option<Game>.None
        );

        if (!moveResult.IsSome) return BadRequest("Couldn't perform the move.");
        
        GameRepository.SaveGame(game);
        return Ok();
    }
    
    private Option<SessionCookieData> ExtractSession(string? session)
    {
        if( session is null ) return Option<SessionCookieData>.None;
        var jsonData = Encoding.UTF8.GetString(Convert.FromBase64String(session));
        return JsonSerializer.Deserialize<SessionCookieData>(jsonData);
    }
}
