using Microsoft.AspNetCore.Mvc;

namespace SaverSearch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    // Common properties or helper methods for controllers (e.g., Mediator access)
}
