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
using System.Globalization;
using Newtonsoft.Json;
using System.Xml.Serialization;

namespace ProOnePal.Controllers
{
    public class TournamentsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        
        // GET: Tournaments
        public ActionResult Index()
        {
            return View(db.Tournaments.ToList());
        }

        // GET: Tournaments/Details/5
        public ActionResult Details(int? id)
        {
            List<string> groups;
            string stage = "";
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            Tournament tournament       = db.Tournaments.Find(id);
            tournament.results          = db.Results.Where(x => x.fixture.tournamentId == tournament.id).ToList();
            stage                       = Helper.getStageFromTournament(tournament, db);
            tournament.enteredTeams     = Helper.getenterdTeams(tournament, db, stage);
            if(stage == "Done")
                tournament.enteredTeams.ElementAt(0).tournamentStats.FirstOrDefault(x =>
                x.tournamentName == tournament.name).group = "W";

            foreach (var team in tournament.enteredTeams)
                team.tournamentStats    = db.teamTournamentStats.Where(x => x.teamId == team.id).ToList();

            groups = Helper.getDisctinctgroups(db, tournament);
            if (stage.Equals("GS"))
            {
                if (Helper.doTeamsAlreadyHabeGroups(tournament.enteredTeams, tournament) == false)
                    groups = Helper.assignTeamsToGroups(tournament, tournament.stages).ToList();
            }
            if (stage.Equals("QF"))
                return RedirectToAction("QFinal", new {id = tournament.id });
            if (stage.Equals("F"))
                return RedirectToAction("Final", new { id = tournament.id });
            if (stage.Equals("Done"))
            {
                return RedirectToAction("Done", new { id = tournament.id });
            }

            db.SaveChanges();
            ViewBag.Groups = new SelectList(groups);
            if (tournament == null)
                return HttpNotFound();
            return View(tournament);
        }

        public ActionResult Done(int? id)
        {
            var tourn = db.Tournaments.Find(id);
            tourn.enteredTeams = Helper.getenterdTeams(tourn, db, "Done");
            var team = db.Teams.Find(tourn.enteredTeams.ToList().First().id);
            team.players = Helper.ArrangeByGaoals(db.Players.Where(x => x.teamId == team.id).ToList(), db); // Arrange by all goals
            var list = new List<Char>();
            if (team == null)
                return HttpNotFound();

            foreach (var letter in Helper.lastFiveResults(team.name, db).ToString())
                list.Add(letter);
            list.Reverse();
            list = list.Take(5).ToList();
            list.Reverse();
            ViewBag.recent = list;
            ViewBag.tourn = tourn.name;
            return View(team);
        }

        public ActionResult QFinal(int id)
        {
            Tournament tournament               = db.Tournaments.Find(id);
            Dictionary<string, string> images = Helper.getImagePaths(db);
            Helper.prepTournament(tournament, db);
            ViewBag.Images = images;
            return View(tournament);
        }
        public ActionResult Final(int id)
        {
            Tournament tournament = db.Tournaments.Find(id);
            Dictionary<string, string> images = Helper.getImagePaths(db);
            Helper.prepTournament2(tournament, db);
            ViewBag.Images = images;
            return View(tournament);
        }

        // GET: Tournaments/Create
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Tournaments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "id,name")] Tournament tournament)
        {
            if (!Helper.GetTournamentName(db).Contains(tournament.name))
            { 
                if (ModelState.IsValid)
                {
                    db.Tournaments.Add(tournament);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(tournament);
        }

        // GET: Tournaments/Edit/5
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            return View(tournament);
        }

        // POST: Tournaments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "id,maxGames,maxTeams,maxStages,name")] Tournament tournament)
        {
            if (ModelState.IsValid)
            {
                db.Entry(tournament).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(tournament);
        }

        // GET: Tournaments/Delete/5
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Tournament tournament = db.Tournaments.Find(id);
            if (tournament == null)
            {
                return HttpNotFound();
            }
            return View(tournament);
        }

        // POST: Tournaments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            Tournament tournament = db.Tournaments.Find(id);
            db.Tournaments.Remove(tournament);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        [Authorize(Roles = "Admin")]
        public ActionResult AddTeam(int? id, string error)
        {
            ViewBag.teamId          = new SelectList(db.Teams, "id", "name");
            var tournament          = db.Tournaments.Find(id);
            var stage               = Helper.getStageFromTournament(tournament, db);
            tournament.enteredTeams = Helper.getenterdTeams(tournament, db,stage);
            ViewBag.error = error;
            return View(tournament);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public ActionResult AddTeam(int? id, int teamId)
        {
            ViewBag.teamId = new SelectList(db.Teams, "id", "name");
            var images              = Helper.getImagePaths(db);
            Team team               = db.Teams.Find(teamId);
            var tournament          = db.Tournaments.Find(id);
            var tournCount          = db.Tournaments.Count();
            var statsForTeamCount   = db.teamTournamentStats.ToList().Where(x => x.teamId == teamId).Count();

            if (statsForTeamCount < tournCount && !tournament.enteredTeams.Contains(team))
            {
                team.tournamentStats.Add(new TeamTournamentStat()
                { tournamentName = tournament.name });
                team.players = db.Players.Where(x => x.team.id == team.id).ToList();
                foreach (var player in team.players)
                {
                    db.playerTournamentStats.Add(new PlayerTournamentStat()
                    { tournamentName = tournament.name, playerId = player.Id, player = player });
                }
                ViewBag.success = "Team added succesfully";
                tournament.enteredTeams.Add(team);
            }
            else {
                ViewBag.error = "Team already in tournamet";
                ViewBag.images = images;
                return View(tournament);
            }
            db.SaveChanges();
            return View(tournament);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult AddFixture(int ? id)
        {
            Fixture fixture     = new Fixture();
            var tournament      = db.Tournaments.Find(id);
            fixture.tournament  = tournament;
            var teams           = Helper.getTeamsByTournamentName(db,tournament.name);  
            var teamNames       = Helper.getTeamNames(teams);

            ViewBag.HomeTeam    = new SelectList(teamNames);
            ViewBag.AwayTeam    = new SelectList(teamNames);
            ViewBag.errors      = "";
            
            return View(fixture);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public ActionResult AddFixture([Bind(Include = "id,time,homeTeam,awayTeam,date,pitch")] Fixture fixture, int? id)
        {
            var tournament          = db.Tournaments.Find(id);
            var teams               = Helper.getTeamsByTournamentName(db, tournament.name);
            var teamNames           = Helper.getTeamNames(teams);
            var errors              = new List<string>();
            string maxPlayedMessage = "";

            Helper.assignTournamentsToFixtures(db);
            tournament.results      = db.Results.Where(x => x.fixture.tournament.name== tournament.name).ToList();
            fixture.tournamentId    = (int)id;
            fixture.tournament      = tournament;
            fixture.fixtureName     = Helper.createFixtureName(fixture);

            Helper.verifyFixture(db, fixture,errors,teams,tournament,ref maxPlayedMessage);

            if (errors.Count == 0)
            {
                if (ModelState.IsValid)
                {
                    fixture.stage = Helper.getStageFromTournament(tournament,db);
                    if(!Helper.IsFixtureInDb(fixture,db,tournament))
                        db.Fixtures.Add(fixture);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            ViewBag.errors = errors;
            ViewBag.HomeTeam = new SelectList(teamNames);
            ViewBag.AwayTeam = new SelectList(teamNames);
            return View(fixture);
        }

        [Authorize(Roles = "Admin")]
        public ActionResult RemoveTeam(int? id,string name)
        {
            ViewBag.teamId = new SelectList(db.Teams, "id", "name");
            var team        = db.Teams.Find(id);
            var tourn       = db.Tournaments.ToList().First(x => x.name == name);
            var stage       = Helper.getStageFromTournament(tourn, db);
            var teamStat    = db.teamTournamentStats.First(x => x.tournamentName == name && x.teamId == id);

            tourn.enteredTeams = Helper.getenterdTeams(tourn, db,stage);
            tourn.enteredTeams.Remove(team);
            if (teamStat.gamesPlayed == 0)
            {
                db.teamTournamentStats.Remove(teamStat);
                db.SaveChanges();
                return RedirectToAction("AddTeam", new { id = tourn.id, error = "Team Removed" });
            }
            else
            {
                return RedirectToAction("AddTeam", new { id = tourn.id,error = "You cannot remove team that's already played" });
            }
            
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
