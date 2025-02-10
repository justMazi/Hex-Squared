using Domain;
using Domain.Players;
using Domain.Players.MCTS;

namespace Tests;

public class PathFinderTests
{
    [Test]
    public void Test1()
    {
        var pathfinder = new PathFinder();
        
        var radius = 7;
        var game = new Game(new GameId("absd"), radius, typeof(PathFinderHeuristic));
        
        foreach (var i in Enumerable.Range(0,3).ToList())
        {
            var rotatedHexes = MctsPlayer.HexRotation.RotateHexes(game.Hexagons, i);
            var rotatedArray = game.To2DArray(rotatedHexes);
            game.PrintRaw2DArray(rotatedArray);
            pathfinder.HasPath(rotatedArray, 1, i);
        }
    } 
    
    [Test]
    public void Test3()
    {
        var pathfinder = new PathFinder();
        
        var radius = 7;
        var game = new Game(new GameId("absd"), radius, typeof(PathFinderHeuristic));
        
        foreach (var i in Enumerable.Range(1,3).ToList())
        {
            var rotatedHexes = MctsPlayer.HexRotation.RotateHexes(game.Hexagons, 0);
            var rotatedArray = game.To2DArray(rotatedHexes);
            game.PrintRaw2DArray(rotatedArray);
            pathfinder.HasPath(rotatedArray, i, 0);
        }
    }
    
    [Test]
    public void Test2()
    {
        var pathfinder = new PathFinder();
        
        var radius = 7;
        var game = new Game(new GameId("absd"), radius, typeof(PathFinderHeuristic));
        
        foreach (var rotation in Enumerable.Range(0,3).ToList())
        {
            foreach (var player in Enumerable.Range(1, 3).ToList())
            {
                var rotatedHexes = MctsPlayer.HexRotation.RotateHexes(game.Hexagons, rotation);
                var rotatedArray = game.To2DArray(rotatedHexes);
                game.PrintRaw2DArray(rotatedArray);
                pathfinder.HasPath(rotatedArray, player, rotation);
            }
        }
    }
}