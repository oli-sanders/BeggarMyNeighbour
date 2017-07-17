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
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CardGames.BeggarMyNeighbour.Scoreboard.Models;
using System.Text;

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Services
{
    public interface IVerifyService
    {

    }


    public class VerifyService:IVerifyService
    {
        private IConnectionFactory _factory;
        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;
        private ScoreBoardContext _context;

        public VerifyService(IConnectionFactory factory, ScoreBoardContext context)
        {
            _factory = factory;
            
            _context = context;
            _connection = _factory.CreateConnection();
            _connection.ConnectionShutdown += _connection_ConnectionShutdown;
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "verify_response_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _consumer = new  EventingBasicConsumer(_channel);
            _consumer.Received += _consumer_Received;
            
            _channel.BasicConsume(queue: "verify_response_queue", noAck: true, consumer: _consumer);

        }

        private void _connection_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            //  throw new NotImplementedException();
            Console.WriteLine(e.ReplyText);

        }

        private void _consumer_Received(object sender, BasicDeliverEventArgs ea)
        {
            var body = ea.Body;
            var message = Encoding.UTF8.GetString(body);
            // Display message

            Console.WriteLine(" [x] Received {0}", message);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<VerifyResponse>(message);

            if(response.success)
            {
                var score = _context.Scores.Find(response.id);
                score.Verified = response.Verified;
                _context.SaveChanges();
            }

            Console.WriteLine($" [x] received verification {response.id}. The game was verified : {response.success}");

            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
    }
}
