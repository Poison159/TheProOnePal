using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProOnePal.Models
{
    
    public class StatsByTournament
    {
        public string tournName { get; set; }
        public double wins { get; set; }
        public double losses { get; set; }
        public double draws { get; set; }
    }
}