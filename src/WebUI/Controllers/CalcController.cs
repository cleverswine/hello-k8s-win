using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Surescripts.WebUI.Hubs;
using Surescripts.WebUI.Models;
using Surescripts.WebUI.Services;

namespace Surescripts.WebUI.Controllers
{
    [Route("/api/calc")]
    [ApiController]
    public class CalcController : Controller
    {
        private readonly ILogger<CalcController> logger;
        private readonly IHubContext<StatusHub> hubContext;
        private readonly IMQClient mq;

        public CalcController(ILogger<CalcController> logger, IHubContext<StatusHub> hubContext, IMQClient mq)
        {
            this.logger = logger;
            this.hubContext = hubContext;
            this.mq = mq;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Calculation calc)
        {
            calc.Id = Guid.NewGuid().ToString();
            logger.LogInformation("got a new message to process. ID: " + calc.Id);
            calc.StartTime = DateTime.UtcNow;
            calc.CallbackUrl = Url.Action("Post");
            await hubContext.Clients.All.SendAsync("StatusUpdate", calc);
            mq.Send(calc);
            return Ok(calc);
        }

        [HttpPut]
        public async Task<IActionResult> Put([FromBody]Calculation calc)
        {
            logger.LogDebug($"got an update for message {calc.Id}: {calc.Status}% done");
            await hubContext.Clients.All.SendAsync("StatusUpdate", calc);
            return Ok();
        }
    }
}
