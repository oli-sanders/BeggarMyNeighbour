using System;
using Microsoft.Extensions.Logging;

namespace beggar
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

            ILoggerFactory loggerFactory = new LoggerFactory();

            loggerFactory
                .AddConsole()
                .AddDebug();

            var players = 4;
            
            switch(algorithm)
            {
                case "Best":
                    var compute = new BindBeggarAlgorithm(loggerFactory.CreateLogger("Compute"),new Random() ,players, user);
                    compute.Run();
                    break;

            }
        }
    }

}