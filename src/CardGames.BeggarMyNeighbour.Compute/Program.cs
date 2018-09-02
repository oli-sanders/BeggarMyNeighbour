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
using Microsoft.Extensions.Logging;

namespace CardGames.BeggarMyNeighbour.Compute
{
    /// <summary>
    /// Main Program Class runs beggar according to enviromental varibles or default
    /// </summary>
    class Program
    {
        /// <summary>
        /// Main Entrypoint
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var algorithm = Environment.GetEnvironmentVariable("Algorithm") ?? "Best";
            var user = Environment.GetEnvironmentVariable("BeggarUser");
            var url = Environment.GetEnvironmentVariable("ScoreboardUrl") ?? "http://beggar-api.o-os.uk";

            ILoggerFactory loggerFactory = new LoggerFactory();

            loggerFactory
                .AddConsole()
                .AddDebug();

            var players = 4;
            
            switch(algorithm)
            {
                case "Best":
                    var compute = new BindBeggarAlgorithm(loggerFactory.CreateLogger("Compute"),new Random() ,players, user, url);
                    compute.Run();
                    break;
            }
        }
    }
}
