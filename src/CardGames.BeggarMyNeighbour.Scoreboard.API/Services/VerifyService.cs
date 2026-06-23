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
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using CardGames.BeggarMyNeighbour.Scoreboard.Models;

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Services
{
    public interface IVerifyService { }

    public class VerifyService : IVerifyService
    {
        private readonly IConnectionFactory _factory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<VerifyService> _logger;
        private IConnection _connection;
        private IModel _channel;
        private EventingBasicConsumer _consumer;

        public VerifyService(IConnectionFactory factory, IServiceScopeFactory scopeFactory, ILogger<VerifyService> logger)
        {
            _factory = factory;
            _scopeFactory = scopeFactory;
            _logger = logger;

            try
            {
                _connection = _factory.CreateConnection();
                _connection.ConnectionShutdown += ConnectionShutdown;
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(queue: "verify_response_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                _consumer = new EventingBasicConsumer(_channel);
                _consumer.Received += ConsumerReceived;

                _channel.BasicConsume(queue: "verify_response_queue", autoAck: false, consumer: _consumer);
            }
            catch (Exception ex)
            {
                // The API should still start (and accept scores) even if the
                // message bus is not yet available - verification simply waits.
                _logger.LogWarning(ex, "Could not connect to the message bus; verification is disabled.");
            }
        }

        private void ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            _logger.LogWarning("Verify bus connection shut down: {Reason}", e.ReplyText);
        }

        private void ConsumerReceived(object sender, BasicDeliverEventArgs ea)
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation("Received verification result {Message}", message);

            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<VerifyResponse>(message);

            if (response.success)
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ScoreBoardContext>();
                var score = context.Scores.Find(response.id);
                if (score != null)
                {
                    score.Verified = response.Verified;
                    context.SaveChanges();
                }
            }

            _logger.LogInformation("Verification for {Id} complete; verified: {Success}", response.id, response.success);

            _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
    }
}
