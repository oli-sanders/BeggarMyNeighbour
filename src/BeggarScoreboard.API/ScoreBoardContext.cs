using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Scoreboard.API
{
    public class ScoreBoardContext : DbContext
    {

        public ScoreBoardContext(DbContextOptions<ScoreBoardContext> options)
            : base(options)
        { }
        


        public DbSet<Score> Scores { get; set; }
        


    }
}
