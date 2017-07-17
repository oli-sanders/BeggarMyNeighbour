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
using CardGames.BeggarMyNeighbour.Scoreboard.Models;

namespace CardGames.BeggarMyNeighbour.Scoreboard.API
{
    public static class ScoreExtensions
    {
    
       public static VerifyRequest ToVerifyRequest (this Score score)
        {
            return new VerifyRequest
            {
                id = score.id,
                Deck = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(score.Deck),
                Lenght = score.Lenght,
                Players = score.Players
            };
        }

        public static Score ToScore (this ScoreRequest request)
        {
            var score = new Score
            {
                User = String.IsNullOrEmpty(request.User) ? "anon e mouse" : request.User,
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
}