using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Scoreboard.API
{
    public class ScoreRequest
    {
        
        public string User { get; set; }
        public int Lenght { get; set; }
        public List<int> Deck { get; set; }
        public int Players { get; set; }
    }

    public class ScoreResponse
    {
        public string User { get; set; }
        public int Lenght { get; set; }
        public List<int> Deck { get; set; }
        public bool IsVerified { get; set; }
        public int Players { get; set; }
    }

    public static class ScoreExtensions
    {
        public static Score ToScore (this ScoreRequest request)
        {
            var score = new Score
            {
                User = String.IsNullOrEmpty(request.User) ? "anon o mouse" : request.User,
                Submitted = DateTime.UtcNow,
                Deck = Newtonsoft.Json.JsonConvert.SerializeObject(request.Deck),
                Lenght = request.Lenght,
                Players = request.Players
            };
            return score;
        }

        public static ScoreResponse ToScoreResponse(this Score value)
        {
            var scoreResponse = new ScoreResponse
            {
                User = value.User,
                Lenght = value.Lenght,
                Deck = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(value.Deck),
                IsVerified = (value.Verified != null),
                Players = value.Players                
            };
            return scoreResponse;
        }
    }

    public class Score
    {
        [Key]
        public int id { get; set; }

        public string Deck { get; set; }

        public string User { get; set; }
        public int Lenght { get; set; }
        public int Players { get; set; }

        public DateTime Submitted { get; set; }
        public DateTime? Verified { get; set; }
    }
}