using Domain;
using Domain.Players;
using Microsoft.AspNetCore.Mvc;

namespace HexSquared.Api.v1;

[ApiController]
[Route("api/v1/")]
public class AiDiscoveryController : ControllerBase
{
    [HttpGet("AI")]
    public ActionResult<IEnumerable<string>> GetInheritors()
    {
        var inheritorTypes = InheritorHelpers.GetInheritors<AiPlayer>();
        var inheritorNames = inheritorTypes.Select(t => t.Name).ToList();
        return Ok(inheritorNames);
    }
    
}
