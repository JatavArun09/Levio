using Levio.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Levio.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet("testing")]
        [Authorize]
        public IActionResult Get()
        {
            return Ok("Authenticated User");
        }


        [HttpGet("admin")]
        [Authorize(Roles = "HRAdmin")]
        public IActionResult AdminOnly()
        {
            return Ok("HR Admin Access");
        }
    }
}
