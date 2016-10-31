using Discord;
using Discord.Commands;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemeBot {

    class Bot {

        DiscordClient discord;
        CommandService commands;

        public Bot() {
            //Define discord
            discord = new DiscordClient(x => {
                x.LogLevel = LogSeverity.Info;
                x.LogHandler = Log;
            });

            //Define commands prefix
            discord.UsingCommands(x => {
                x.PrefixChar = '.';
                x.AllowMentionPrefix = true;
            });

            //Define commands
            commands = discord.GetService<CommandService>();
            RegisterPurgeCommand();
            RegisterVoteCommand();


            discord.Log.Message += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            discord.ExecuteAndWait(async () => {
                await discord.Connect("MjQwMzEzNzE4NTk5MDU3NDA5.CvUSVw.WyXvoZIGC6eATIssc8XMrxjrf1I", TokenType.Bot);
            });

        }

        private void RegisterPurgeCommand() {
            commands.CreateCommand("purge")
                .Do(async (e) => {
                    if (e.User.Id == (ulong)191731936497106945 || e.User.Id == (ulong)176185451902664705) {
                        Message[] messages;
                        messages = await e.Channel.DownloadMessages(100);

                        await e.Channel.DeleteMessages(messages);
                    }
                });
        }


        private void RegisterVoteCommand() {
            commands.CreateCommand("vote")
                .Parameter("Param1", ParameterType.Optional)
                .Parameter("Param2", ParameterType.Optional)
                .Do(async c => {
                    await c.Channel.SendMessage(GetMessageForRank(InterpretParameters(c.Channel, c.Server.Users, (string)c.GetArg("Param1"), (string)c.GetArg("Param2"))));
                });
        }

        private string[] InterpretParameters(Channel c, IEnumerable<User> users, string p1, string p2) {
            bool p1IsName = false;
            ulong id = 0;
            foreach (User u in users) {
                if (u.Name.Equals(p1)) {
                    p1IsName = true;
                    id = u.Id;
                    continue;
                }
            }

            if (p1IsName) return new string[] { id + "", p2 };
            else return new string[] { GetLastId(c) + "", p1 };
        }

        private ulong GetLastId(Channel c) {
            throw new NotImplementedException();
        }

        private string GetMessageForRank(string[] paramValues) {
            MySqlParameter[] parameters = new MySqlParameter[2];
            if (parameters == null) return "Error";

            SqlQuery sqlQuery = new SqlQuery("localhost", "root", Password.pass, "memerank");

            parameters[0] = new MySqlParameter("?ID", MySqlDbType.Int64);
            parameters[0].Value = Int64.Parse(paramValues[0]);
            parameters[1] = new MySqlParameter("?Rank", MySqlDbType.Double);
            parameters[1].Value = Double.Parse(paramValues[1]);
            sqlQuery.SetQuery("INSERT INTO Ranks (ID, Rank) VALUES (?ID, ?Rank)", parameters);
            sqlQuery.ExecuteQuery(QueryTypes.INSERT);

            return parameters[0] + " " + parameters[1];
        }

        private string GetUserRank(ulong userId) {
            MySqlParameter[] parameters = new MySqlParameter[1];
            SqlQuery sqlQuery = new SqlQuery("localhost", "root", "[poassword", "memerank");
            parameters[0] = new MySqlParameter("?ID", MySqlDbType.Int64);
            parameters[0].Value = (Int64)userId;
            sqlQuery.SetQuery("SELECT * FROM Ranks WHERE ID = ?ID", parameters);
            MySqlDataReader results = sqlQuery.ExecuteQuery(QueryTypes.SELECT);
            return "";
        }

        private void Log(object sender, LogMessageEventArgs e) {
            Console.WriteLine(e.Message);
        }

    }

}
