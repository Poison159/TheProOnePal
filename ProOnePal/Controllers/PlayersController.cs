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
using PagedList;

namespace ProOnePal.Controllers
{
    public class PlayersController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Players
      
        public ActionResult Index(string searchName, string team, int ? page)
        {
            if (searchName != null)
            {
                page = 1;
            }

            var teamList = new List<string>();
            var players = from cr in db.Players select cr;
            var test = players.ToList();

            Helper.assignTeamsToPlayers(db,players.ToList());
            var teamquery = from gmq in db.Players
                            orderby gmq.team.name
                            select gmq.team.name;
            if (teamquery.ToList().Count == 0) // go to Create because there is no team
                return PartialView("_NoTeams");

            if (!string.IsNullOrEmpty(searchName))
                players = players.Where(x => x.name.Contains(searchName));

            if (!string.IsNullOrEmpty(team))
                players = players.Where(x => x.team.name.Equals(team));

            teamList.AddRange(teamquery.Distinct());
            ViewBag.team = new SelectList(teamList);
            ViewBag.num = 0;

            int pageSize = 5;
            int pageNumber = (page ?? 1);

            return View(players.OrderBy(x => x.teamId).ToList().ToPagedList(pageNumber,pageSize));
        }
        // GET: Players/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var players = from cr in db.Players select cr;

            Player player = db.Players.Find(id);
            if (player == null)
            {
                return HttpNotFound();
            }
            player.tournamentStats = db.playerTournamentStats.Where(X =>X.playerId == player.Id).ToList();
            var plrResStat  = db.playerResultStats.Where(x => x.playerId == player.Id).ToList();
            ViewBag.goals   = Helper.getPlayerGoalsByTournament(plrResStat,db);
            ViewBag.avrg = Helper.AvrgGoalsPerGame(player.Id, db, plrResStat).ToString("0.0");
            
            return View(player);
        }

        // GET: Players/Create
        public ActionResult Create()
        {
            ViewBag.teamId = new SelectList(db.Teams, "id", "name");
            Player player = new Player();
            var positions = Helper.ReturnPositions();
            ViewBag.position = new SelectList(positions);
            return View(player);
        }

        // POST: Players/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,teamId,name,age,position,imgPath,imageUpload")] Player player)
        {
            ViewBag.teamId = new SelectList(db.Teams, "id", "name", player.teamId);
            var positions = Helper.ReturnPositions();
            ViewBag.position = new SelectList(positions);
            if (ModelState.IsValid)
            {
                if (!Helper.CheckIfPlayerInTeam(player, db.Teams, db.Players)) // check if playert already in team
                {
                    if (player.imageUpload != null)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(player.imageUpload.FileName);
                        string extention = Path.GetExtension(player.imageUpload.FileName);
                        fileName = player.name + extention;
                        player.imgPath = "~/Content/imgs/" + fileName;
                        player.imageUpload.SaveAs(Path.Combine(Server.MapPath("~/Content/imgs/"), fileName));
                    }
                    db.Players.Add(player);
                }  
                else
                {
                    ViewBag.error = "Player already exists";
                    return View(player);
                }
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(player);
        }

        // GET: Players/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Player player = db.Players.Find(id);
            if (player == null)
            {
                return HttpNotFound();
            }
            ViewBag.teamId = new SelectList(db.Teams, "id", "name", player.teamId);
            return View(player);
        }

        // POST: Players/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,teamId,name,age,position,imgPath,imageUpload")] Player player)
        {
            if (ModelState.IsValid)
            {
                if (player.imageUpload != null)
                {
                    string fileName = Path.GetFileNameWithoutExtension(player.imageUpload.FileName);
                    string extention = Path.GetExtension(player.imageUpload.FileName);
                    fileName = player.name + extention;
                    player.imgPath = "~/Content/imgs/" + fileName;
                    player.imageUpload.SaveAs(Path.Combine(Server.MapPath("~/Content/imgs/"), fileName));
                }

                db.Entry(player).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.teamId = new SelectList(db.Teams, "id", "name", player.teamId);
            return View(player);
        }

        // GET: Players/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Player player = db.Players.Find(id);
            if (player == null)
            {
                return HttpNotFound();
            }
            return View(player);
        }

        // POST: Players/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Player player = db.Players.Find(id);
            db.Players.Remove(player);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult AddPlayers()
        {
            var filePath = @"C:\Users\Siya\Downloads\ProOnePal\ProOnePal\obj\Debug\TestPlayers.txt";
            var myFile = System.IO.File.Open(filePath, FileMode.Open);
            using (StreamReader myStream = new StreamReader(myFile))
            {
                if (System.IO.File.Exists(filePath))
                {
                    string line;
                    while ((line = myStream.ReadLine()) != null)
                    {
                        var randId          = Helper.RandomTeamsId(db);
                        var elems           = line.Split(',');
                        var player          = new Player() { name = elems[0], age = int.Parse(elems[1]) };
                        player.position     = Helper.RandomPosition();
                        player.teamId = randId;
                        db.Players.Add(player);
                        db.SaveChanges();
                    }
                }
            }
            return View(db.Teams.ToList());
        }

        [HttpPost]
        public string GetPlayerPath(string playerName)
        {
            Dictionary<string,string> players = Helper.getPlayerImagePaths(db);
            var ret = players[playerName].Trim('~');
            return ret;
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
