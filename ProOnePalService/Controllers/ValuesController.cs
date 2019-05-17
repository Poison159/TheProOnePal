using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using ProOnePal.Models;
using System.Configuration;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.IO;
using ProOnePalService.Models;

namespace ProOnePalService.Controllers
{
    public class ValuesController : ApiController
    {
        ServDataContext db = new ServDataContext();
        string connectionString = ConfigurationManager.ConnectionStrings["OnePalDb"].ConnectionString;
        // GET api/values
        public IEnumerable<Team> Get()
        { return db.Teams; }

        // GET api/values/5
        public Team Get(int id)
        { return db.Teams.First(x => x.id == id); }

        

        [Route("api/teams")]
        public string GetTeamName(int id)
        {
            string queryString = "SELECT name FROM dbo.Teams WHERE Id =" + id.ToString();
            
            List<string> list = new List<string>();
            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(queryString, connection);
                connection.Open();
                using (var reader = command.ExecuteReader())
                    while (reader.Read())
                        list.Add(reader[0].ToString());
            }
            return list.FirstOrDefault();
        }



        // POST api/values
        public void Post([FromBody]string value)
        {}

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {}

        // DELETE api/values/5
        public void Delete(int id)
        {}
    }
}
