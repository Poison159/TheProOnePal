using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ProOnePal.Models;
using System.Data.Entity;
using System.Text;
using System.Xml.Serialization;
using System.IO;

namespace ProOnePal.Models
{
    public class Helper
    {
        public static int Max { get; internal set; }

        public static TeamStats getAllTeamsStats(IEnumerable<Team> teams)
        {
            var teamStats = new TeamStats();

            foreach (var team in teams)
            {
                teamStats.wins += getGames(team, "won");
                teamStats.losses += getGames(team, "lost");
                teamStats.draws += getGames(team, "drawn");
            }

            return teamStats;
        }

        internal static void assignTeamsToPlayers(ApplicationDbContext db, List<Player> players)
        {
            var teams = db.Teams.ToList();
            foreach (var player in players)
                player.team = teams.FirstOrDefault(x => x.id == player.teamId);
        }

        internal static Dictionary<string, string> getImagePaths(ApplicationDbContext db)
        {
            var ret = new Dictionary<string, string>();
            foreach (var team in db.Teams.ToList())
                ret.Add(team.name, team.imgPath.TrimStart('~'));
            return ret;
        }

        internal static void assignPlayersToScorers(List<PlayerResultStat> scorers, ApplicationDbContext db)
        {
            foreach (var resStat in scorers)
                resStat.player = db.Players.Find(resStat.playerId);
        }

        internal static List<Player> ArrangeByGaoals(List<Player> players, ApplicationDbContext db)
        {
            List<Player> retPlayers             = new List<Player>();
            Dictionary<int, int> playerGaols    = new Dictionary<int, int>();
            foreach (var player in players)
            {
                var goals = 0;
                goals = getAllPlayerGoals(db.playerResultStats.Where(x => x.playerId == player.Id).ToList());
                playerGaols.Add(player.Id, goals);
            }

            foreach (var item in playerGaols.OrderByDescending(x => x.Value))
                retPlayers.Add(players.FirstOrDefault(x => x.Id == item.Key));
            return retPlayers;
        }

        internal static List<StatsByTournament> getTeamTournStats(ApplicationDbContext db, Team team, List<string> tournList)
        {
            List<Tournament> tournaments = db.Tournaments.ToList();
            List<StatsByTournament> statsByPercentage = new List<StatsByTournament>();
            
                foreach (var item in tournaments)
                {
                    var gamesPlayed = getGamesPlayedInTournament(team, item.name, db);
                    if (gamesPlayed == 0)
                        {
                            StatsByTournament SBT = new StatsByTournament
                            {
                                tournName = item.name,
                                wins = 0,
                                draws = 0,
                                losses = 0
                            };
                            statsByPercentage.Add(SBT);

                    }
                    else {
                        var gamesWon    = getGamesWonInTournament(team, db.Tournaments.ToList().First(x => x.name == item.name));
                        var gamesDrawn  = getGamesDrawnInTournament(team, db.Tournaments.ToList().First(x => x.name == item.name));
                        var gamesLost   = getGamesLostInTournament(team, db.Tournaments.ToList().First(x => x.name == item.name));
                        StatsByTournament SBT = new StatsByTournament
                        {
                            tournName   = item.name,
                            wins        = getPercentage(gamesPlayed, gamesWon),
                            draws       = getPercentage(gamesPlayed, gamesDrawn),
                            losses      = getPercentage(gamesPlayed, gamesLost)
                        };
                        statsByPercentage.Add(SBT);
                    }
                 }
            return statsByPercentage;
        }

        internal static List<string> getAlltournamets(DbSet<Tournament> tournaments)
        {
            List<string> tournNames = new List<string>();
            foreach (var item in tournaments)
            {
                tournNames.Add(item.name);
            }
            return tournNames;
        }

        internal static List<string> getDisctinctgroups(ApplicationDbContext db, Tournament tounament)
        {
            var list = new List<string>();
            var statquery = from gmq in db.teamTournamentStats
                            where gmq.tournamentName == tounament.name
                            orderby gmq.@group
                            select gmq.@group;

            list.AddRange(statquery.Distinct());
            return list;
        }

        internal static void prepTournament(Tournament tournament, ApplicationDbContext db)
        {
            var stage               = getStageFromTournament(tournament, db);
            tournament.enteredTeams = getenterdTeams(tournament, db,stage);
            Helper.assignFixturesToResults(db.Results.ToList(), db);
            tournament.results      = db.Results.ToList().Where(x => x.fixture.tournamentId == tournament.id).ToList();
            int qFinalFixturesCount = db.Fixtures.ToList().Where(x => x.tournamentId == tournament.id && x.stage == "QF").Count();

            if (qFinalFixturesCount == 0)
                CreateFixtures(tournament, db);

            tournament.fixtures     = db.Fixtures.ToList().Where(x => x.tournamentId == tournament.id && x.stage == "QF" && x.Played == "No").ToList();
            tournament.results      = db.Results.ToList().Where(x => x.fixture.tournamentId == tournament.id && x.fixture.stage == "QF").ToList();
            getStatsbyName(tournament.enteredTeams, tournament.name); // prioritise current tournStat

            foreach (var res in tournament.results)
            {
                res.scorers = db.playerResultStats.Where(x => x.resultId == res.id).ToList();
                foreach (var scorer in res.scorers)
                {
                    scorer.player = db.Players.ToList().FirstOrDefault(x => x.Id == scorer.playerId);
                    scorer.result = null;
                }
            }
        }

        private static void assignFixturesToResults(List<Result> results, ApplicationDbContext db)
        {
            foreach (var item in results)
                item.fixture = db.Fixtures.Find(item.fixtureId);
        }

        internal static void prepTournament2(Tournament tournament, ApplicationDbContext db)
        {
            var stage               = getStageFromTournament(tournament, db);
            tournament.enteredTeams = getenterdTeams(tournament, db, stage);
            Helper.assignFixturesToResults(db.Results.ToList(), db);
            tournament.results      = db.Results.ToList().Where(x => x.fixture.tournamentId == tournament.id).ToList();
            int qFinalFixturesCount = db.Fixtures.ToList().Where(x => x.tournamentId == tournament.id && x.stage == "F").Count();

            if (qFinalFixturesCount == 0)
                CreateFixtures(tournament, db);

            tournament.fixtures = db.Fixtures.ToList().Where(x => x.tournamentId == tournament.id && x.stage == "F" && x.Played == "No").ToList();
            tournament.results = db.Results.ToList().Where(x => x.fixture.tournamentId == tournament.id && x.fixture.stage == "F").ToList();
            getStatsbyName(tournament.enteredTeams, tournament.name); // prioritise current tournStat

            foreach (var res in tournament.results)
            {
                res.scorers = db.playerResultStats.Where(x => x.resultId == res.id).ToList();
                foreach (var scorer in res.scorers)
                {
                    scorer.player = db.Players.ToList().FirstOrDefault(x => x.Id == scorer.playerId);
                    scorer.result = null;
                }
            }
        }


        private static void CreateFixtures(Tournament tourn, ApplicationDbContext db)
        {
            var stage           = getStageFromTournament(tourn, db);
            tourn.enteredTeams  = Helper.getenterdTeams(tourn, db,stage);
            tourn.fixtures      = db.Fixtures.ToList().Where(x => x.tournamentId == tourn.id).ToList();
            tourn.results       = db.Results.ToList().Where(x => x.fixture.tournamentId == tourn.id).ToList();
            int start = 0;
            int mid = (tourn.enteredTeams.Count / 2);
            while (start < tourn.enteredTeams.Count / 2)
            {
                Fixture fix = new Fixture() {
                    date = DateTime.Now,
                    pitch = "madiba",
                    Played = "No",
                    tournamentId = tourn.id,
                    time = DateTime.Now,
                    tournament = tourn,
                };
                if (stage == "QF")
                {
                    fix.homeTeam = tourn.enteredTeams.ElementAt(start).name;
                    fix.awayTeam = tourn.enteredTeams.ElementAt(mid).name;
                }
                else
                {
                    fix.homeTeam = tourn.enteredTeams.ElementAt(mid).name;
                    fix.awayTeam = tourn.enteredTeams.ElementAt(start).name;
                }
                fix.stage = getStageFromTournament(tourn, db);
                fix.fixtureName = createFixtureName(fix);
                db.Fixtures.Add(fix);
                db.SaveChanges();
                start++;
                mid++;
            }
        }

        internal static void ChangeNameInFixtures(ApplicationDbContext db, string oldName, string newName)
        {
            var fixtures = db.Fixtures.ToList();
            foreach (var fix in fixtures)
            {
                if (fix.homeTeam == oldName)
                    fix.homeTeam = newName;
                else
                    fix.awayTeam = newName;
                db.Entry(fix).State = EntityState.Modified;
            }
        }

        private static List<Team> getLastTwo(List<Result> last4Results, List<Team> enteredTeams,ApplicationDbContext db)
        {
            string winnerSide = "";
            List<Team> lastTwo = new List<Team>();
            foreach (var res in last4Results)
            {
                if (res.homeGoals > res.awayaGoals)
                    winnerSide = "home";
                else
                    winnerSide = "away";

                if(winnerSide == "home")
                    lastTwo.Add(getTeamByName(res.fixture.homeTeam,db));
                else
                    lastTwo.Add(getTeamByName(res.fixture.awayTeam, db));
            }
            return lastTwo;
        }

        private static List<Result> getlastFourResultsts(List<Result> list)
        {
            List<Result> retResults = new List<Result>();
            list.Reverse();
            for (int i = 0; i < 5; i++)
                retResults.Add(list[i]);
            return retResults;
        }

        private static int getAllPlayerGoals(List<PlayerResultStat> list)
        {
            int sum = 0;
            foreach (var stat in list)
                sum += stat.goals;
            return sum;
        }

        internal static void assignTournamentsToFixtures(ApplicationDbContext db)
        {
            foreach (var fixture in db.Fixtures.ToList())
            {
                var toun = db.Tournaments.Find(fixture.tournamentId);
                fixture.tournament = toun;
            }
            db.SaveChanges();
        }

        internal static Dictionary<string, int> getPlayerGoalsByTournament(List<PlayerResultStat> plrResStats, ApplicationDbContext db)
        {
            Dictionary<string, int> tournGaols = new Dictionary<string, int>();
            if (plrResStats == null)
                return tournGaols;
            int sum;
            foreach (var tournName in GetTournamentName(db))
            {
                sum = 0;
                foreach (var plrStat in plrResStats)
                {
                    plrStat.result                      = db.Results.FirstOrDefault(x => x.id == plrStat.resultId);
                    plrStat.result.fixture              = db.Fixtures.FirstOrDefault(x => x.id == plrStat.result.fixtureId);
                    plrStat.result.fixture.tournament   = db.Tournaments.FirstOrDefault(x => x.id == plrStat.result.fixture.tournamentId);
                    if (plrStat.result.fixture.tournament.name == tournName)
                        sum++;
                }
                tournGaols.Add(tournName, sum);
            }
            return tournGaols;
        }

        internal static void assignTournamentsToFixtures2(List<Result> results, ApplicationDbContext db)
        {
            foreach (var res in results)
            {
                var toun = db.Tournaments.Find(res.fixture.tournamentId);
                res.fixture.tournament = toun;
            }
            db.SaveChanges();
        }

        public static void getStatsbyName(List<Team> teams, string tournamentName)
        {
            foreach (var team in teams)
            {
                foreach (var stat in team.tournamentStats.ToList().Where(x => x.tournamentName == tournamentName))
                {
                    if (stat.tournamentName == tournamentName)
                    {
                        team.tournamentStats.Insert(0, stat);
                        break;
                    }
                }
            }
        }

        internal static bool CheckIfPlayerInTeam(Player player, DbSet<Team> teams, DbSet<Player> players)
        {
            var team        = teams.Find(player.teamId);
            team.players    = players.Where(x => x.team.id == team.id).ToList();
            if (Helper.getPlayerNames(team.players).Contains(player.name))
                return true;
            return false;
        }

        internal static bool DoesTeamHaveMin(int teamId, ApplicationDbContext db)
        {
            var team = db.Teams.Find(teamId);
            if (team.players.Count == 3)
                return true;
            return false;
        }

        internal static List<string> getPlayerNames(List<Player> list)
        {
            var playerNames = new List<string>();
            foreach (var item in list)
                playerNames.Add(item.name);
            return playerNames;
        }

        internal static List<Team> getenterdTeams(Tournament tournament, ApplicationDbContext db,string stage)
        {
            var teams           = new List<Team>();
            var filteredStats   = new List<TeamTournamentStat>();
            if (stage != "GS")
                filteredStats  = db.teamTournamentStats.Where(x => x.tournamentName
                    == tournament.name && x.group == stage).ToList();
            if(stage == "Done")
                filteredStats = db.teamTournamentStats.Where(x => x.tournamentName
                   == tournament.name && x.group == "W").ToList();
            else
                filteredStats = db.teamTournamentStats.Where(x => x.tournamentName
                   == tournament.name).ToList();

            foreach (var team in db.Teams)
            {
                foreach (var stat in filteredStats)
                {
                    if (stat.teamId == team.id)
                        teams.Add(team);
                }
            }
            return teams;
        }

        internal static List<string> getStages()
        {
            return new List<string>() { "Group Stages", "Knock Out", "Last_16", "Q_Final", "Final" };
        }

        internal static List<string> GetTournamentName(ApplicationDbContext db)
        {
            var tournaments = new List<string>();
            foreach (var tourn in db.Tournaments.ToList())
                if (!tournaments.Contains(tourn.name))
                    tournaments.Add(tourn.name);
            db.SaveChanges();
            return tournaments;
        }

        public static Team getTeamByName(string temaName, ApplicationDbContext db)
        {
            return db.Teams.ToList().FirstOrDefault(x => x.name == temaName);
        }

        public static int wasTeamHomeOraway(string teamName, Fixture fix)
        {
            if (teamName == fix.homeTeam)
                return 1;
            return 0;
        }

        public static int getGames(Team team, string kind)
        {
            int result = 0;
            if (kind == "won")
            {
                foreach (var stat in team.tournamentStats)
                {
                    result += stat.gamesWon;
                }
                return result;
            } else if (kind == "lost")
            {
                foreach (var stat in team.tournamentStats)
                    result += stat.gamesLost;
                return result;
            } else if (kind == "drawn") {
                foreach (var stat in team.tournamentStats)
                    result += stat.gamesLost;
                return result;
            }
            else
                return result;
        }

        internal static Dictionary<string, string> getPlayerImagePaths(ApplicationDbContext db)
        {
            Dictionary<string, string> dic  = new Dictionary<string, string>();
            var players                     = db.Players.ToList();
            foreach (var player in players)
                dic.Add(player.name, player.imgPath);
            return dic;
        }

        internal static bool IsInputValid(string homeGoals, string awayGoals, ref int goalsForHome, ref int goalsForAway)
        {
            if (!string.IsNullOrEmpty(homeGoals) && !string.IsNullOrEmpty(awayGoals))
            {
                if (int.TryParse(homeGoals, out goalsForHome) && int.TryParse(awayGoals, out goalsForAway))
                    return true;
                else
                    return false;
            }
            return false;
        }
        public static StringBuilder lastFiveResults(string teamName, ApplicationDbContext db)
        {

            StringBuilder list = new StringBuilder();
            var Results = db.Results.ToList();

            foreach (var item in Results)
                item.fixture = db.Fixtures.Find(item.fixtureId);
            var teamRes = Results.Where(x => x.fixture.homeTeam == teamName
            || x.fixture.awayTeam == teamName);
            foreach (var result in teamRes)
            {
                if (result.homeGoals == result.awayaGoals)
                {
                    list.Append("D");
                    continue;
                }
                if (Helper.wasTeamHomeOraway(teamName, result.fixture) == 1)
                    if (result.homeGoals > result.awayaGoals)
                        list.Append('W');
                    else
                        list.Append('L');
                if (Helper.wasTeamHomeOraway(teamName, result.fixture) == 0)
                    if (result.homeGoals < result.awayaGoals)
                        list.Append('W');
                    else
                        list.Append('L');
            }
            return list;
        }

        internal static bool IsFixtureInDb(Fixture fixture, ApplicationDbContext db, Tournament tournament)
        {
            var dbFixtures = db.Fixtures.ToList().Where(x => x.tournamentId == tournament.id);
            foreach (var fix in dbFixtures)
            {
                if ((fix.homeTeam == fixture.homeTeam && fix.awayTeam == fixture.awayTeam) ||
                    fix.awayTeam == fixture.homeTeam && fix.homeTeam == fixture.awayTeam)
                {
                    return true;
                }
            }
            return false;
        }

        internal static PlayerStats getPlayerStats(List<IEnumerable<KeyValuePair<Player, int>>> key_pairs)
        {
            PlayerStats plStat = new PlayerStats();
            foreach (var key_pair in key_pairs)
            {
                foreach (var item in key_pair)
                {
                    if (item.Key.age <= 19)
                        plStat.teangoals++;
                    if (item.Key.age > 19 && item.Key.age <= 25)
                        plStat.youngGoals++;
                    if (item.Key.age > 25 && item.Key.age <= 30)
                        plStat.adultGoals++;
                    if (item.Key.age > 30)
                        plStat.adultGoals++;
                }
            }
            return plStat;
        }

        internal static void verifyFixture(ApplicationDbContext db, Fixture fixture, List<string> errors, List<Team> teams, Tournament tournament, ref string maxPlayedMessage)
        {
            var teamsInFixture  = getTeams(fixture.homeTeam, fixture.awayTeam, db.Teams.ToList());
            var dbFixtures      = db.Fixtures.ToList();
            
            if (fixture.date <= DateTime.Now)
                errors.Add("Date field must be a future date");
            if (!Helper.areTeamsInSameGroup(fixture.homeTeam, fixture.awayTeam, tournament, db))
                errors.Add("Home & Away team not in same gruop");
            if (fixture.homeTeam == fixture.awayTeam)
                errors.Add("Home & Away team are the same");
            if (Helper.haveTeamsPlayed(teamsInFixture, tournament.id, db))
                errors.Add("teams have already played");
            if (Helper.hasTeamPlayedMaxGames(teams.FirstOrDefault(x => x.name == fixture.homeTeam), db, tournament, ref maxPlayedMessage)
                        && Helper.hasTeamPlayedMaxGames(teams.FirstOrDefault(x => x.name == fixture.awayTeam), db, tournament, ref maxPlayedMessage))
                errors.Add(maxPlayedMessage);

            foreach (var fix in dbFixtures.ToList().Where(x => x.tournamentId == tournament.id))
            {
                if (fix.homeTeam == fixture.homeTeam && fix.awayTeam == fixture.awayTeam)
                {
                    errors.Add("Fixture already exits");
                    break;
                }
            }
        }

        internal static string getStageFromTournament(Tournament tournament, ApplicationDbContext db)
        {
            
            if (tournament.results.Count >= 12 && tournament.results.Count < 14)
                return "QF";
            if (tournament.results.Count == 14)
                return "F";
            if (tournament.results.Count == 15)
                return "Done";
            else
                return "GS";
        }

        public static double AvrgGoalsPerGame( int playerId, ApplicationDbContext db, List<PlayerResultStat> plaResStats)
        {
            var res = 0.0;
            var player          = db.Players.Find(playerId);
            var teamName        = db.Teams.Find(player.teamId).name;
            var totalGoals      = getTotalGoals(plaResStats.ToList(), playerId);
            var results         = db.Results.ToList();
            Helper.assignFixturesToResults(results, db);
            results             = results.Where(x => x.fixture.awayTeam 
                    == teamName || x.fixture.homeTeam == teamName).ToList();
            if (results.Count() != 0 && totalGoals != 0)
                res =  Convert.ToDouble(results.Count()) / Convert.ToDouble(totalGoals);
            return  res;
        }
        public static int getTotalGoals(List<PlayerResultStat> pl,int playerId)
        {
            var totalGoals = 0;
            foreach (var res in pl)
                totalGoals += res.goals;
            return totalGoals;
        }


        public static void AssignWinningTeam(Tournament tournament,ApplicationDbContext db)
        {
            var lastResult      = tournament.results.Last();
            tournament.fixtures = db.Fixtures.ToList().Where(x => x.tournamentId == tournament.id).ToList();
            var homeTeam        = tournament.enteredTeams.FirstOrDefault(x => x.name == lastResult.fixture.homeTeam);
            var awayTeam        = tournament.enteredTeams.FirstOrDefault(x => x.name == lastResult.fixture.awayTeam);

            if (lastResult.homeGoals > lastResult.awayaGoals)
                homeTeam.tournamentStats.FirstOrDefault(x => 
                x.tournamentName == tournament.name).group = "W";
            else
                awayTeam.tournamentStats.FirstOrDefault(x => 
                x.tournamentName == tournament.name).group = "W";
        }
        

        public static int getGoalDiff(int gf, int ga)
        {
            return (gf - ga);
        }

        public static Dictionary<string,TeamStats> getTeamsObjStat(ApplicationDbContext db,int id)
        {
            Team team = db.Teams.Find(id);
            Dictionary<string, TeamStats> dictionary = new Dictionary<string, TeamStats>();
                dictionary.Add(team.name, getRatio(lastFiveResults(team.name, db)));
            return dictionary;
        }

        public static TeamStats getRatio(StringBuilder sb)
        {
            var str = sb.ToString();
            TeamStats ts = new TeamStats();
            foreach (var letter in str)
            {
                if(letter == 'W')
                    ts.wins = countDistinct(str, letter);
                if (letter == 'D')
                    ts.draws = countDistinct(str, letter);
                if(letter == 'L')
                    ts.losses = countDistinct(str, letter);
            }
            return ts;
        }

      

        public static int countDistinct(string str,char c)
        {
            int res = 0;
            foreach (var letter in str)
                if (letter == c)
                    res++;
            return res;
        }


        public static Ratio ReturnRatio(IEnumerable<Player> list)
        {
            var ecxel       = list.Where(x => x.position == "ST");
            var good        = list.Where(x => x.position == "LW" || x.position == "RW");
            var poor        = list.Where(x => x.position == "CAD" || x.position == "LB" || x.position == "RB");
            var fair        = list.Where(x => x.position == "MD" || x.position == "CAM");

            Ratio obj       = new Ratio();
            obj.skikers     = ecxel.Count();
            obj.wingers     = good.Count();
            obj.midfilders  = fair.Count();
            obj.defenders   = poor.Count();

            return obj;
        }

        internal static int RandomTeamsId(ApplicationDbContext db)
        {
            var rand        = new Random();
            var teams       = db.Teams.ToList();
            foreach (var item in teams)
                item.players = db.Players.ToList().Where(x => x.teamId == item.id).ToList();
            var list = teams.Where(x => x.players.Count < 3).ToList();
            return list.OrderBy(x => +rand.Next()).First().id;
        }

        internal static IEnumerable<int> getTeamIds(List<Team> list)
        {
            foreach (var item in list)
                yield return item.id;
        }

        internal static string RandomPosition()
        {
            Random rand     = new Random();
            var positions   = ReturnPositions();
            return positions.OrderBy(x => rand.Next()).First();
        }

        internal static List<string> getFixturesString(List<Fixture> list)
        {
            List<string> fixtureList = new List<string>();
            if (list != null) {
                foreach (var item in list)
                {
                    string temp = "";
                    temp = item.homeTeam + "," + item.awayTeam + "," + item.date.ToShortDateString() + "," + item.pitch;
                    fixtureList.Add(temp);
                }
            }
            return fixtureList;
        }
        public static bool doTeamsAlreadyHabeGroups(List<Team> teams, Tournament tournament)
        {
            foreach (var team in teams)
            {
                if (team.tournamentStats.FirstOrDefault(x => x.tournamentName == tournament.name).group == null)
                    return false;
            }
            return true;
        }

        internal static bool areTeamsInSameGroup(string homeTeam, string awayTeam,Tournament tourn, ApplicationDbContext db)
        {
            var homeStat        = db.teamTournamentStats.First(x => x.team.name == homeTeam &&
                                  x.tournamentName == tourn.name);
            var awayStat        = db.teamTournamentStats.First(x => x.team.name == awayTeam &&
                                x.tournamentName == tourn.name);
            if (homeStat.group == awayStat.group)
                return true;
            else
                return false;
        }

        public static List<string> ReturnPositions()
        {
            List<string> positions = new List<string>()
            {
                "ST","DF","LW","RW","CAD","MD","RB","LB","GK"
            };
            return (positions);
        }

        public static List<string> getTeamNames(List<Team> teams)
        {
            List<string> teamNames = new List<string>();
            foreach (var team in teams)
            {
                teamNames.Add(team.name);
            }
            return teamNames;
        }

        internal static string createFixtureName(Fixture fixture)
        {
            return fixture.homeTeam + "," + fixture.awayTeam + "," + fixture.date;
        }

        private static IEnumerable<int> getIdList(List<Team> teams)
        {
            foreach (var team in teams)
            {
                yield return team.id;
            }
        }
        internal static void SaveData(ApplicationDbContext db, Result result)
        {
            var fixture     = db.Fixtures.Find(result.fixtureId);
            fixture.Played  = "Yes";
            db.SaveChanges();
            var tournament  = db.Tournaments.Find(fixture.tournamentId);
            assignStats(result, db, tournament.name, fixture);
        }

        public static void assignStats(Result result, ApplicationDbContext db,string tournamentName, Fixture fixture)  
        {
            List<Team> teamInFixture    = findTeams(result,db.Teams.ToList());
            Team homeTeam               = teamInFixture.ElementAt(0);
            Team awayTeam               = teamInFixture.ElementAt(1);
            
            // assigns all stats && saves Changes
            assignDefaultStats(tournamentName,homeTeam,awayTeam ,result,db); 
            
            Tournament tourn = db.Tournaments.ToList().First(x => x.name == tournamentName);
            tourn.results = db.Results.Where(x => x.fixture.tournamentId == tourn.id).ToList();
            // chamges stats of top two team in each group of tournament
            if (tourn.results.Count == 12 || tourn.results.Count == 14 || tourn.results.Count == 15)
                Helper.ChangeTeamStatus(db.teamTournamentStats.Where(x => x.tournamentName 
                == tournamentName).ToList(),tourn,db); 
        }

        public static string SerializeResults(List<Result> results)
        {
            XmlSerializer ser = new XmlSerializer(typeof(Result));
            string xml = "";
            foreach (var res in results)
            {
                using (StringWriter sw = new StringWriter())
                {
                    ser.Serialize(sw, res);
                    xml = sw.ToString();
                }
            }
            return xml;
        }

        
        private static void ChangeTeamStatus(List<TeamTournamentStat> tournStats,Tournament tournament,ApplicationDbContext db)
        {
            var group = getStageFromTournament(tournament, db);
            if (group == "GS" || group == "QF")
            {
                var letters = new List<string> { "A", "B", "C", "D", "E", "F", "G" };
                foreach (var letter in AssignGroups(letters, tournament))
                {
                    var stats = tournStats.Where(x => x.group
                    == letter).ToList().OrderBy(a => a.points).Reverse().Take(2);
                    if (stats.ElementAt(0).group != getStageFromTournament(tournament, db))
                        foreach (var stat in stats)
                            stat.group = getStageFromTournament(tournament, db);
                }
            }
            if (group == "F")
            {
                var stats = tournStats.Where(x => x.group
                    == "QF").ToList().OrderBy(a => a.points).Reverse().Take(2);
                if (stats.ElementAt(0).group != getStageFromTournament(tournament, db))
                    foreach (var stat in stats)
                        stat.group = getStageFromTournament(tournament, db);
            }
            db.SaveChanges();
        }

        public static bool IsthereSameFixtureInThatDay(Fixture fix,List<Fixture> dbFixtures)
        {
            foreach (var fixture in dbFixtures)
            {
                if (fixture.date == fix.date)
                    if (fixture.homeTeam == fix.homeTeam && fixture.awayTeam == fix.awayTeam)
                        return true;
            }
            return false;
        }


        public static int getGamesPlayedInTournament(Team team, string tournName,ApplicationDbContext db)
        {
            Tournament tourn = db.Tournaments.ToList().First(x => x.name == tournName);
            TeamTournamentStat tournStat = null;
            if (hasTeamPlayedTourn(team,tournName))
                tournStat = db.teamTournamentStats.ToList().First(x => x.tournamentName == tourn.name && x.teamId == team.id);
            else
                return 0;
            return tournStat.gamesPlayed;
        }

        public static bool hasTeamPlayedTourn(Team team,string tournName)
        {
            foreach (var item in team.tournamentStats)
            {
                if (item.tournamentName == tournName)
                    return true;
            }
            return false;
        }

        public static int getGamesWonInTournament(Team team, Tournament tourn)
        {
            return team.tournamentStats.First(x => x.tournamentName == tourn.name).gamesWon;   
        }

        public static int getGamesLostInTournament(Team team, Tournament tourn)
        {
            return team.tournamentStats.First(x => x.tournamentName == tourn.name).gamesLost;
        }

        public static int getGamesDrawnInTournament(Team team, Tournament tourn)
        {
            return team.tournamentStats.First(x => x.tournamentName == tourn.name).gamesDrawn;
        }


        public static double getPercentage(int gamesPlayed, int entity)
        {
            var percent = (Convert.ToDouble(entity) / Convert.ToDouble(gamesPlayed)) * (100);
            return percent;
        }

        public static List<Team> getTeamsByTournamentName(ApplicationDbContext db, string tournamentname)
        {
            var retTeams = new List<Team>();
            
            foreach (var stat in db.teamTournamentStats.ToList())
            {
                if(stat.tournamentName == tournamentname)
                {
                    var team = db.Teams.Find(stat.teamId);
                    retTeams.Add(team);
                }
            }
            return retTeams.OrderBy(x => x.tournamentStats.FirstOrDefault(y => y.tournamentName == tournamentname).group).ToList();
        }

        public static void assignDefaultStats (string tournamentName,Team homeTeam, Team  awayTeam,Result result,ApplicationDbContext db)
        {
            TeamTournamentStat homeStats = db.teamTournamentStats.ToList().First(x => x.teamId == homeTeam.id &&
                                                                x.tournamentName == tournamentName);
            TeamTournamentStat awayStats = db.teamTournamentStats.ToList().First(x => x.teamId == awayTeam.id &&
                                                                x.tournamentName == tournamentName);
            homeStats.gamesPlayed++;
            homeStats.goalsFor      += result.homeGoals;
            homeStats.goalsAgainst  += result.awayaGoals;
            homeStats.getGoalDiff();

            awayStats.gamesPlayed++;
            awayStats.goalsFor      += result.awayaGoals;
            awayStats.goalsAgainst  += result.homeGoals;
            awayStats.getGoalDiff();
            PopulateGamesWon(homeStats, awayStats, result,db);
        }

        private static void PopulateGamesWon(TeamTournamentStat homeTeam, TeamTournamentStat awayTeam, Result result,ApplicationDbContext db)
        {
            if (result.homeGoals > result.awayaGoals)
            {
                homeTeam.gamesWon++;
                homeTeam.points     += 3;
                awayTeam.gamesLost++;
            }
            else if (result.homeGoals < result.awayaGoals)
            {
                awayTeam.gamesWon++;
                awayTeam.points     += 3;
                homeTeam.gamesLost++;
            }
            else {
                homeTeam.gamesDrawn++;
                awayTeam.gamesDrawn++;
                homeTeam.points++;
                awayTeam.points++;
            }
            db.SaveChanges();
        }

        private static List<Team> findTeams(Result result, List<Team> teams)
        {
            var teamsInFixture  = new List<Team>();
            string homeTeamName = result.fixture.fixtureName.Split(',').ElementAt(0);
            string awayTeamName = result.fixture.fixtureName.Split(',').ElementAt(1);

            Team awayTeam       = teams.First(x => x.name == awayTeamName);
            Team homeTeam       = teams.First(x => x.name == homeTeamName);

            teamsInFixture.Add(homeTeam);
            teamsInFixture.Add(awayTeam);
            return teamsInFixture;
        }

        private static List<Team> getTeams(string homeTeamName,string awayTeamName, List<Team> teams)
        {
            List<Team> retTeams = new List<Team>();
            foreach (var item in teams)
                if (item.name == homeTeamName || item.name == awayTeamName)
                    retTeams.Add(item);
            return retTeams;
        }

        public static Fixture findFixture(string fitureName,List<Fixture> fixtures)
        {
            var array = fitureName.Split(',');
            foreach (var item in fixtures)
            {
                if (item.homeTeam == array[0] && item.awayTeam == array[1])
                {
                    return item;
                }
            }
            return null;
        }


        internal static IEnumerable<string> assignTeamsToGroups(Tournament tourn, Dictionary<string, List<Team>> groups)
        {
            var letters = new List<string> { "A", "B", "C", "D", "E", "F", "G" };
            var lettersToAssign = AssignGroups(letters, tourn);
            foreach (var letter in letters.Take(lettersToAssign.Count()))
            {
                groups.Add(letter, tourn.enteredTeams.Where(x =>
                x.tournamentStats.Where(a => a.group == letter) != null).ToList());
            }
            return lettersToAssign;
        }

        public static IEnumerable<string> AssignGroups(List<string> letters,Tournament tourn)
        {
            var teams = tourn.enteredTeams;
            IEnumerable<int> teamIdList = getIdList(teams);
            Random rand                 = new Random();
            var randIds                 = teamIdList.OrderBy(x => rand.Next()).ToArray();
            var lettersToAssign         = letters.Take(teams.Count / 4);
            if(!AllTeamsEneteredHaveGrops(teams))
                giveGroupsRandom(teams, lettersToAssign, randIds,tourn);
            return lettersToAssign;
        }

        public static void giveGroupsRandom(List<Team> teams, IEnumerable<string> lettersToAssign, int[] randIds, Tournament tourn)
        {
            if (isReady(teams.Count))
            {
                int j = 0;
                foreach (var group in lettersToAssign)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        teams.First(x => x.id == randIds[j])
                            .tournamentStats.FirstOrDefault(x => x.tournamentName
                            == tourn.name).group = group;
                        j++;
                    }
                }
            }
        }
        public static void assignResultStat(Player player, ApplicationDbContext db,Result result,Tournament tourn, bool og)
        {
            var playerStat = player.tournamentStats.First(x => x.tournamentName == tourn.name);
            playerStat.goals++;
            PlayerResultStat ResultStat = new PlayerResultStat
            {
                playerId = player.Id,
                player = player,
                resultId = result.id,
                result = result
            };
            if (og == false)
                ResultStat.goals++;
            else
                ResultStat.goals--;
            db.playerResultStats.Add(ResultStat);
            db.SaveChanges();
        }

        public static bool haveTeamsPlayed(List<Team> teams,int tournId, ApplicationDbContext db)
        {
            var results = db.Results.ToList();
            foreach (var res in results.Where(x => x.fixture.tournamentId == tournId))
            {
                if (getTeamNames(teams).Contains(res.fixture.homeTeam) &&
                getTeamNames(teams).Contains(res.fixture.awayTeam))
                    return true;                       
            }
            return false;
        }

        public static bool hasTeamPlayedMaxGames(Team team, ApplicationDbContext db, Tournament tourn, ref string maxPlayedMessage)
        {
            if (getStageFromTournament(tourn,db) == "GS")
                if (team.tournamentStats.FirstOrDefault(x => x.tournamentName == tourn.name).gamesPlayed <= 2)
                    return false;
                else
                {
                    maxPlayedMessage = team.name + " has played max times";
                    return true;
                }

            return false;
        }

        public static bool haveTeamsPlayedBefore(Fixture fixture,ApplicationDbContext db)
        {
            var prevFixtures = db.Fixtures.ToList();
            foreach (var item in prevFixtures)
                if (item.homeTeam == fixture.homeTeam && item.awayTeam == fixture.awayTeam
                    && item.tournament.name == fixture.tournament.name)
                    return true;
            return false;
        }

        private static bool isReady(int count)
        {
            if (count == 4 || count == 8 || count == 16 || count == 32)
                return true;
            return false;
        }

        private static bool AllTeamsEneteredHaveGrops(List<Team> teams)
        {
            foreach (var team in teams)
                foreach (var stat in team.tournamentStats)
                    if (stat.group == null)
                        return false;
            return true;
        }


    }
}