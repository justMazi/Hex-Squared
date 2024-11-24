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
    public ActionResult<Game> GetGameState(string id)
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
            var sessionData = JsonSerializer.Deserialize<SessionCookieData>(session);
                
            if (sessionData?.Id == id)
            {
                return BadRequest("You have already picked a color");
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
                var sessionData = new SessionCookieData(updated.Id.ToString(), color);

                var jsonData = JsonSerializer.Serialize(sessionData);
                HttpContext.Response.Cookies.Append("hex_session", jsonData,   new CookieOptions
                {
                    HttpOnly = false, 
                    Secure = true,    
                    SameSite = SameSiteMode.None,
                    IsEssential = true,
                    MaxAge = TimeSpan.FromDays(365),
                    Expires = DateTimeOffset.Now.AddDays(3),
                    Domain = "localhost"
                });

                Console.WriteLine("adding hex session cookie");

                return Ok(updated);
            },
            None: () => BadRequest("Color already taken.")
        );
    }
    
    [HttpPost("game/{id}/move")]
    public IActionResult Move(string id, [FromQuery] int q, [FromQuery] int r, [FromQuery] int s)
    {
        var gameId = new GameId(id);
        var game = GameService.GetOrCreate(gameId);

        HttpContext.Request.Cookies.TryGetValue("hex_session", out var session);

        var hexagonToPick = game.Hexagons.FirstOrDefault(h => h.Q == q && h.R == r && h.S == s);
        if (hexagonToPick is null)
        {
            return BadRequest("Couldn't perform the move.");
        }
        var moveResult = ExtractSession(session).BiBind(
            data => game.Move(new HumanPlayer(data.PlayerNumber), hexagonToPick),
            () => Option<Game>.None
        );

        return moveResult.Match<IActionResult>(game1 =>
        {
            GameRepository.SaveGame(game1);
            return Ok();
        }, BadRequest);
    }
    
    [HttpPost("game/{id}/reset")]
    public IActionResult Reset(string id)
    {
        // Retrieve the existing game
        var gameId = new GameId(id);
        var existingGame = GameService.Get(gameId);

        // Check if the game exists
        return existingGame.Match<IActionResult>(game1 =>
        {
            var resetGame = game1.Reset();
            GameRepository.SaveGame(resetGame);
            return Ok();
        }, BadRequest);
    }

    [HttpPost("game/{id}/fill-with-ai")]
    public IActionResult FillWithAi(string id, [FromQuery] int index)
    {
        var gameId = new GameId(id);
        var game = GameService.GetOrCreate(gameId);
        var updatedGame = game.FillWithAi();
        return updatedGame.Match<IActionResult>(game1 =>
        {
            GameRepository.SaveGame(game1);
            return Ok();
        }, BadRequest);
    }
    
    private Option<SessionCookieData> ExtractSession(string? session)
    {
        if( session is null ) return Option<SessionCookieData>.None;
        return JsonSerializer.Deserialize<SessionCookieData>(session);
    }
}
