using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ProOnePal.Models;
using System.IO;
using System.Text;

namespace ProOnePal.Controllers
{
    public class TeamsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        // GET: Teams
        public ActionResult Index(string searchName, string kasi)
        {
            var kasiList    = new List<string>();
            var teams       = from cr in db.Teams select cr;
            var kasiquery   = from gmq in db.Teams
                            orderby gmq.kasi
                            select gmq.kasi;
            
            if (!string.IsNullOrEmpty(searchName))
                teams = teams.Where(x => x.name.Contains(searchName));

            if (!string.IsNullOrEmpty(kasi))
            {
                teams = teams.Where(x => x.kasi.
                ToString().Equals(kasi));
            }
           
            kasiList.AddRange(kasiquery.Distinct());
            ViewBag.kasi = new SelectList(kasiList);
            db.SaveChanges();
            return View(teams);
        }
        // GET: Teams/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            Team team       = db.Teams.Find(id);
            team.tournamentStats = db.teamTournamentStats.ToList().Where(x => x.teamId == team.id).ToList();
            team.players    = Helper.ArrangeByGaoals(db.Players.Where(x => x.team.id == id).ToList(),db); // Arrange by all goals
            var list        = new List<Char>();
            if (team == null)
                return HttpNotFound();
            
            foreach (var letter in Helper.lastFiveResults(team.name, db).ToString())
                list.Add(letter);
            list.Reverse();
            list = list.Take(5).ToList();
            list.Reverse();
            ViewBag.recent = list;
            var tournList = Helper.getAlltournamets(db.Tournaments);
            var teamTournStats = Helper.getTeamTournStats(db,team,tournList);
            
            ViewBag.PercList = teamTournStats;


            return View(team);
        }
        
        public ActionResult CurrentTeamPlayers(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var players = db.Players.Where(x => x.team.id == id);
            if (players == null)
            {
                return HttpNotFound();
            }
            ViewBag.teamName = db.Teams.Find(id).name;
            return View(players);
        }
        
        public ActionResult AddPlayer(int? id)
        {
            Player player       = new Player();
            var positions       = Helper.ReturnPositions();
            var playerTeam      = db.Teams.Find(id);
            player.team         = playerTeam;
           

            ViewBag.position    = new SelectList(positions);
            return View(player);
        }



        [HttpPost]
        public ActionResult AddPlayer([Bind(Include = "name,age,position,imgPath,imageUpload")] Player player, int? id)
        {
            var positions       = Helper.ReturnPositions();
            var playerTeam      = db.Teams.Find(id);
            ViewBag.position    = new SelectList(positions);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            player.teamId       = (int)id;
            player.team         = playerTeam;
            if (ModelState.IsValid)
            {
                if (player.imageUpload != null)
                {
                    string fileName = Path.GetFileNameWithoutExtension(player.imageUpload.FileName);
                    string extention = Path.GetExtension(player.imageUpload.FileName);
                    fileName = player.name + DateTime.Now.ToString("yymmssfff") + extention;
                    player.imgPath = "~/Content/imgs/" + fileName;
                    player.imageUpload.SaveAs(Path.Combine(Server.MapPath("~/Content/imgs/"), fileName));
                }
                db.Players.Add(player);
                db.SaveChanges();
                return RedirectToAction("CurrentTeamPlayers", new { id = id });
            }
            return View(player);
        }



        // GET: Teams/Create
        public ActionResult Create()
        {
            Team team = new Team();
            return View(team);
        }

        // POST: Teams/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "id,name,kasi,imgPath,imageUpload")] Team team)
        {
            if (ModelState.IsValid)
            {
                if (team.imageUpload != null)
                {
                    string fileName     = Path.GetFileNameWithoutExtension(team.imageUpload.FileName);
                    string extention    = Path.GetExtension(team.imageUpload.FileName);
                    fileName            = team.name + DateTime.Now.ToString("yymmssfff") + extention;
                    team.imgPath        = "~/Content/imgs/" + fileName;
                    team.imageUpload.SaveAs(Path.Combine(Server.MapPath("~/Content/imgs/"), fileName));
                }
                if (!Helper.getTeamNames(db.Teams.ToList()).Contains(team.name))
                    db.Teams.Add(team);
                else
                {
                    ViewBag.error = "Team already exists";
                    return View(team);
                }
                db.SaveChanges();
                return RedirectToAction("Index");
                
            }
            return View(team);
        }

        // GET: Teams/Edit/5
        [Authorize]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Team team = db.Teams.Find(id);
            if (team == null)
            {
                return HttpNotFound();
            }
            return View(team);
        }

        // POST: Teams/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "id,name,kasi,imgPath,imageUpload")] Team team)
        {
            
            if (ModelState.IsValid)
            {
                if (team.imageUpload != null)
                {
                    string fileName     = Path.GetFileNameWithoutExtension(team.imageUpload.FileName);
                    string extention    = Path.GetExtension(team.imageUpload.FileName);
                    fileName            = team.name + extention;
                    team.imgPath        = "~/Content/imgs/" + fileName;
                    team.imageUpload.SaveAs(Path.Combine(Server.MapPath("~/Content/imgs/"), fileName));
                }
                
                db.Entry(team).State = EntityState.Modified;
                
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(team);
        }

        // GET: Teams/Delete/5
        [Authorize]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Team team = db.Teams.Find(id);
            if (team == null)
            {
                return HttpNotFound();
            }
            return View(team);
        }

        // POST: Teams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Team team = db.Teams.Find(id);
            if (team.players != null)
            {
                foreach (var item in team.players)
                {
                    var player = db.Players.Find(item.Id);
                    player.teamId = 0;
                }
            }
            db.Teams.Remove(team);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Addteams()
        {
            var filePath    = @"C:\Users\Siya\Documents\Visual Studio 2015\Projects\ProOnePal\ProOnePal\obj\Debug\TestTeams.txt";
            var myFile      = System.IO.File.Open(filePath, FileMode.Open);
            using (StreamReader myStream = new StreamReader(myFile))
            {
                if (System.IO.File.Exists(filePath))
                {

                    string line;
                    while ((line = myStream.ReadLine()) != null)
                    {
                        var elems = line.Split(',');
                        var team = new Team() { name = elems[0], kasi = elems[1],
                            imgPath = "~/Content/imgs/" + elems[0] + ".png",
                        };
                        db.Teams.Add(team);    
                    }
                }
            }
            db.SaveChanges();
            return View(db.Teams.ToList());
        }
        public ActionResult ChartTeams(int ? id)
        {
            // Teams players
            var listOfPlayers = db.Players.ToList().Where(x => x.teamId == id);
            // Count the players in each position
            Ratio obj = Helper.ReturnRatio(listOfPlayers);
            // Get all goals for each player
            Dictionary<Player, int> player_goals = new Dictionary<Player, int>();
            foreach (var player in listOfPlayers)
                player_goals.Add(player, getAllGoals(player.tournamentStats));
            // key pair to put all goals according to age
            List<IEnumerable<KeyValuePair<Player, int>>> playerRankList = new List<IEnumerable<KeyValuePair<Player, int>>>();
            // add players with respect to goals scored
            playerRankList.Add(player_goals.Where(x => x.Value > 3));
            playerRankList.Add(player_goals.Where(x => x.Value > 1 && x.Value <=3 ));
            playerRankList.Add(player_goals.Where(x => x.Value == 1));
            playerRankList.Add(player_goals.Where(x => x.Value == 0));
            
            PlayerStats fstats = Helper.getPlayerStats(playerRankList);
            
            ViewBag.francewins = fstats.youngGoals; ViewBag.francedraws = fstats.teangoals;
            ViewBag.francelosses = fstats.adultGoals;

            return View(obj);
        }

        private static int getAllGoals(IEnumerable<PlayerTournamentStat> tStats)
        {            int sum = 0;
            foreach (var stat in tStats)
                sum += stat.goals;
            return sum;
        }


        [HttpPost]
        public string GetTeamPath(string teamName)
        {
            var teams       = Helper.getImagePaths(db);
            return teams[teamName];
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
