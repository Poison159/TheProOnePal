using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ProOnePal.Models;
using System.Web.Http.Cors;

namespace ProOnePal.Controllers
{
    [EnableCors(origins: "http://localhost:4400", headers: "*", methods: "*")]
    public class TeamsWebController : ApiController
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        public List<Team> getTeams(string searchName, string kasi) { 
            var kasiList = new List<string>();
            var teams = from cr in db.Teams select cr;
            var kasiquery = from gmq in db.Teams
                            orderby gmq.kasi
                            select gmq.kasi;
            
                if (!string.IsNullOrEmpty(searchName))
                    teams = teams.Where(x => x.name.Contains(searchName));

                if (!string.IsNullOrEmpty(kasi))
                {
                    teams = teams.Where(x => x.kasi.
                    ToString().Equals(kasi));
                }
            return teams.ToList();
        }
    }
}

