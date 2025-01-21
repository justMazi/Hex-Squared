using System.Text.Json;
using Application.IRepositories;
using Application.Services.Interfaces;
using Domain;
using Domain.Players;
using Domain.Players.MCTS;
using HexSquared.Configuration;
using LanguageExt;
using Microsoft.AspNetCore.Mvc;

namespace HexSquared.Api.v1;

[ApiController]
[Route("api/v1/")]
public class GameController(IGameService gameService, IGameRepository gameRepository, IHexConfiguration configuration) : ControllerBase
{
    private IGameService GameService { get; } = gameService;
    private IGameRepository GameRepository { get; } = gameRepository;

    [HttpGet("game/{id}")]
    public IActionResult GetGameState(string id, [FromQuery] int? size = null, [FromQuery] string? aiType = null)
    {
        var gameId = new GameId(id);
        var game = GameService.GetOrCreate(gameId, size, aiType is not null ? InheritorHelpers.GetInheritors<AiPlayer>().FirstOrDefault(t => t.Name == aiType) : typeof(MctsPlayer));
        return Ok(game);
    }

    [HttpPost("game/{id}/pickColor")]
    public IActionResult PickColor(string id, [FromQuery] int color)
    {
        HttpContext.Request.Cookies.TryGetValue("hex_session", out var session);
        if (session != null)
        {
            var sessionData = JsonSerializer.Deserialize<SessionCookieData>(session);
            

            var gameResult = GameService.Get(new GameId(sessionData.Id)).Match(
                existingGame =>
                {
                    if (existingGame.GameState == GameState.InProgress)
                    {
                        // Return the RedirectResult properly
                        return Redirect($"/{existingGame.Id}");
                    }
                    else if (existingGame.GameState == GameState.Finished)
                    {
                        GameRepository.DeleteGame(existingGame);
                        HttpContext.Response.Cookies.Delete("hex_session");
                    }
                    else if (existingGame.GameState == GameState.WaitingForPlayers)
                    {
                        var updatedGame = existingGame.UnpickColor(sessionData.PlayerNumber);
                        updatedGame.IfSome(game => GameRepository.SaveGame(game));
                        HttpContext.Response.Cookies.Delete("hex_session");
                    }

                    return null; // Default case for void return scenarios
                },
                () => 
                {
                    HttpContext.Response.Cookies.Delete("hex_session");
                    return null;
                }
            );
            if (gameResult != null)
            {
                return gameResult; // Return early if `Match` provides a result
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
                    Domain = configuration.HexCookieDomain
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
        var gameId = new GameId(id);
        var existingGame = GameService.Get(gameId);

        return existingGame.Match<IActionResult>(game =>
        {
            var resetGame = game.Reset();
            GameRepository.SaveGame(resetGame);
            return Ok();
        }, BadRequest);
    }
    
    [HttpPost("game/{id}/concede")]
    public IActionResult Concede(string id)
    {
        var gameId = new GameId(id);
        var game = GameService.GetOrCreate(gameId);

        // Extract session data to identify the player
        HttpContext.Request.Cookies.TryGetValue("hex_session", out var session);

        if (session == null)
        {
            return BadRequest("No session found. Player identification failed.");
        }

        var sessionData = ExtractSession(session);
        
        HttpContext.Response.Cookies.Delete("hex_session");

        return sessionData.Match<IActionResult>(
            Some: data =>
            {
                // Identify the player who is conceding
                var player = new HumanPlayer(data.PlayerNumber);

                // Perform the concede operation on the game
                var updatedGame = game.Concede(player);

                // Save the updated game state
                GameRepository.SaveGame(updatedGame);

                return Ok();
            },
            None: () => BadRequest("Invalid session data.")
        );
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
