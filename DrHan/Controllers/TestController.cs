using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace DrHan.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpPost("test-hangfire")]
        public IActionResult TestHangfire()
        {
            // Test immediate job
            BackgroundJob.Enqueue(() => Console.WriteLine("Hangfire is working! Job executed at: " + DateTime.Now));
            
            // Test delayed job
            BackgroundJob.Schedule(() => Console.WriteLine("Delayed job executed at: " + DateTime.Now), TimeSpan.FromMinutes(1));
            
            return Ok(new { message = "Hangfire jobs queued successfully!" });
        }
    }
} 