/* Copyright (c) 2017 Oliver Sanders

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System.Text;
using CardGames.BeggarMyNeighbour.Scoreboard.Models;
using CardGames.BeggarMyNeighbour.Scoreboard.API.Services;

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Controllers
{
    [Route("api/[controller]")]
    public class ScoresController : Controller
    {
        private ScoreBoardContext _context;
        private ThresholdService _thresholdService;
        private IConnectionFactory _connectionFactory;

        public ScoresController(ScoreBoardContext context, ThresholdService threshold, IConnectionFactory connectionFactory)
        {
            _context = context;
            _thresholdService = threshold;
            _connectionFactory = connectionFactory;
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
            if(id == 5)
            {
                var unverifiedscores = _context.Scores.Where(s => s.Verified == null).ToList();
                foreach(var uv in unverifiedscores)
                {
                    SendGameToVerify(uv);
                }
            }

            return "value";
        }

        // POST api/values
        [HttpPost]
        public async Task<IActionResult> Post([FromBody]ScoreRequest value)
        {
            if (ModelState.IsValid)
            {
                //fix values
                var dbvalue = value.ToScore();

                var current = _thresholdService.UpdateThreshold(dbvalue.Lenght, dbvalue.Players);

                //add and save to db
                _context.Scores.Add(dbvalue);
                await _context.SaveChangesAsync();

                SendGameToVerify(dbvalue);

                return Ok(current);
            }
            else
            {
                return BadRequest(ModelState);
            }
        }

        private void SendGameToVerify(Score dbvalue)
        {
            using (var connection = _connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "verify_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                var message = Newtonsoft.Json.JsonConvert.SerializeObject(dbvalue.ToVerifyRequest());
                var body = Encoding.UTF8.GetBytes(message);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(exchange: "", routingKey: "verify_queue", basicProperties: properties, body: body);
                Console.WriteLine(" [x] Sent {0}", message);
            }
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
