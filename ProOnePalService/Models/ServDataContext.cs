using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using ProOnePal.Models;
using System.Data.SqlClient;
using System.Configuration;

namespace ProOnePalService.Models
{
    public class ServDataContext : DbContext
    {
        public ServDataContext()
        {
            Teams = getTeams();
        }

        string connectionString = ConfigurationManager.ConnectionStrings["OnePalDb"].ConnectionString;

        public List<Team> Teams { get; set; }

        public DbSet<Tournament> Tournaments { get; set; }

        public DbSet<Player> Players { get; set; }

        public DbSet<Fixture> Fixtures { get; set; }

        public DbSet<Result> Results { get; set; }
        public DbSet<TeamTournamentStat> teamTournamentStats { get; set; }
        public DbSet<PlayerTournamentStat> playerTournamentStats { get; set; }
        public DbSet<PlayerResultStat> playerResultStats { get; set; }

        private List<Team> getTeams() {

            string queryString = "SELECT Id, name, kasi, imgPath FROM dbo.Teams;";
            List<Team> teams = new List<Team>();
            using (var connection = new SqlConnection(connectionString))
            {
                var command = new SqlCommand(queryString, connection);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Team team = new Team()
                        {
                            id = Convert.ToInt32(reader[0].ToString()),
                            name = reader[1].ToString(),
                            kasi = reader[2].ToString(),
                            imgPath = reader[3].ToString().Split('~').ElementAt(1)
                        };
                        teams.Add(team);
                    }
                }
            }
            assignPlayersToTeam(teams);
            populateStats(teams);
            foreach (var team in teams)
                populatePlayerStats(team.players);

            return teams;

        }
        private void assignPlayersToTeam(List<Team> teams)
        {
            foreach (var team in teams)
            {
                string playersQuery = "SELECT Id,name,age,imgPath,teamId FROM dbo.Players WHERE teamId = " + team.id.ToString();
                List<Player> players = new List<Player>();
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand(playersQuery, connection);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Player player = new Player()
                            {
                                Id = Convert.ToInt32(reader[0].ToString()),
                                age = Convert.ToInt32(reader[2].ToString()),
                                name = reader[1].ToString(),
                                imgPath = reader[3].ToString().Split('~').ElementAt(1),
                                teamId = team.id,
                            };
                            players.Add(player);
                        }
                    }
                }
                team.players = players;
            }
        }

        public void populateStats(List<Team> teams)
        {

            foreach (var team in teams)
            {
                string teamStats = "SELECT id,teamId,tournamentName,gamesPlayed,gamesWon,gamesDrawn,gamesLost," +
                    "points,goalDiff,goalsFor,goalsAgainst FROM dbo.TeamTournamentStats WHERE teamId = " + team.id.ToString();
                List<TeamTournamentStat> tournStats = new List<TeamTournamentStat>();
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand(teamStats, connection);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            TeamTournamentStat teamStat = new TeamTournamentStat()
                            {
                                id = Convert.ToInt32(reader[0].ToString()),
                                teamId = Convert.ToInt32(reader[1].ToString()),
                                tournamentName = reader[2].ToString(),
                                gamesPlayed = Convert.ToInt32(reader[3].ToString()),
                                gamesWon = Convert.ToInt32(reader[4].ToString()),
                                gamesDrawn = Convert.ToInt32(reader[5].ToString()),
                                gamesLost = Convert.ToInt32(reader[6].ToString()),
                                points = Convert.ToInt32(reader[7].ToString()),
                                goalDiff = Convert.ToInt32(reader[8].ToString()),
                                goalsFor = Convert.ToInt32(reader[9].ToString()),
                                goalsAgainst = Convert.ToInt32(reader[10].ToString()),

                            };
                            tournStats.Add(teamStat);
                        }
                    }
                }
                team.tournamentStats = tournStats;
            }
        }
        public void populatePlayerStats(List<Player> players)
        {

            foreach (var player in players)
            {
                string teamStats = "SELECT id,playerId,tournamentName,goals,gamesPlayed FROM dbo.PlayerTournamentStats WHERE playerId = " + player.Id.ToString();
                var tournStats = new List<PlayerTournamentStat>();
                using (var connection = new SqlConnection(connectionString))
                {
                    var command = new SqlCommand(teamStats, connection);
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            PlayerTournamentStat playerStat = new PlayerTournamentStat()
                            {
                                id = Convert.ToInt32(reader[0].ToString()),
                                playerId = Convert.ToInt32(reader[1].ToString()),
                                tournamentName = reader[2].ToString(),
                                goals = Convert.ToInt32(reader[3].ToString()),
                                gamesPlayed = Convert.ToInt32(reader[4].ToString()),
                            };
                            tournStats.Add(playerStat);
                        }
                    }
                }
                player.tournamentStats = tournStats;
            }
        }

    }
}
