using Microsoft.AspNetCore.Mvc;
using SixDegrees.Model.JSON;
using System;
using System.Threading.Tasks;

namespace SixDegrees.Controllers
{
    [Route("api/[controller]")]
    public class RateLimitController : Controller
    {
        private static readonly TimeSpan MaxRateLimitAge = new TimeSpan(0, 5, 0);

        [HttpGet("status")]
        public async Task<RateLimitResults> GetRateLimitStatus()
        {
            return null;
        }
    }
}
