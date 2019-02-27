using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ProOnePal.Models;
using System.Configuration;
using System.Data.SqlClient;

namespace ProOnePalService.Controllers
{
    public class ValuesController : ApiController
    {
        // GET api/values
        public IEnumerable<string> Get()
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var teamPaths = Helper.getImagePaths(db);
            var connectionString = ConfigurationManager.ConnectionStrings["OnePalDb"].ConnectionString;
            string queryString = "SELECT Id, name FROM dbo.Teams;";
            List<string> list = new List<string>();
            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(queryString, connection);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                         list.Add(reader[0].ToString() + " : " + reader[1].ToString());
                    }
                }
            }
            return list;
        }
        [System.Web.Http.Route("api/getPathfromName")]
        public string getPathfromName(int id,string teamName)
        {
            ApplicationDbContext db = new ApplicationDbContext();
            var teamPaths = Helper.getImagePaths(db);
            return teamPaths[teamName];
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
