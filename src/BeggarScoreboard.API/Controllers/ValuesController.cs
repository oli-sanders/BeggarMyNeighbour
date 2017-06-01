using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Scoreboard.API.Services;

namespace Scoreboard.API.Controllers
{
    [Route("api/[controller]")]
    public class ScoresController : Controller
    {
        private ScoreBoardContext _context;
        private ThresholdService _thresholdService;

        public ScoresController(ScoreBoardContext context, ThresholdService threshold)
        {
            _context = context;
            _thresholdService = threshold;
        }
            

        // GET api/values
        [HttpGet]
        public IActionResult Get()
        {
            //get top 100 scores by lenght
            var scores = _context.Scores.OrderByDescending(s => s.Lenght).Take(100).Select(s => s.ToScoreResponse()).ToList();
            return Ok(scores);
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]ScoreRequest value)
        {
            if(ModelState.IsValid)
            {
                //fix values
                var dbvalue = value.ToScore();

                var current = _thresholdService.UpdateThreshold(dbvalue.Lenght, dbvalue.Players);

                //add and save to db
                _context.Scores.Add(dbvalue);
                await _context.SaveChangesAsync();
                    
                return Ok(current);
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
