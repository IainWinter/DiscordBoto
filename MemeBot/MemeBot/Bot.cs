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
                x.PrefixChar = '!';
                x.AllowMentionPrefix = true;
            });

            //Define commands
            commands = discord.GetService<CommandService>();
            RegisterMemesCommand();
            //RegisterPurgeCommand();
            //RegisterVoteCommand();
            //RegisterGetRankCommand();

            discord.Log.Message += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            discord.ExecuteAndWait(async () => {
                await discord.Connect("MjU4NDM1NTMzNzYyNDYxNjk3.CzJOkA.8kITPt2xLe_D9SGvUUzcXQrEaF8", TokenType.Bot);
            });
        }

        private void RegisterMemesCommand() {
            commands.CreateCommand("countall")
                .Do(async (e) => {
                    await e.Channel.SendMessage(".countall");
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
                    string[] p = InterpretParameters(c.Channel, c.Server.Users, (string)c.GetArg("Param1"), (string)c.GetArg("Param2"));
                    await c.Channel.SendMessage(GetMessageForRank(p, c.Channel, c.Message.User.Id, false));
                });
        }

        private void RegisterGetRankCommand() {
            commands.CreateCommand("rank")
                .Parameter("Name", ParameterType.Required)
                .Do(async c => {
                    await c.Channel.SendMessage(GetUserRank(GetIdFromName(c.Server.Users, (string)c.GetArg("Name")), (string)c.GetArg("Name")));
                });
        }

        private ulong GetIdFromName(IEnumerable<User> users, string name) {
            foreach (User u in users) {
                if (name.Contains(u.Id + "") || u.Name.Equals(name)) {
                    return u.Id;
                }
            }
            return 0;
        }

        private string[] InterpretParameters(Channel c, IEnumerable<User> users, string p1, string p2) {
            bool p1IsName = false;
            ulong id = 0;
            foreach (User u in users) {
                if (u.Name.Replace(" ", "").Equals(p1)) {
                    p1IsName = true;
                    id = u.Id;
                }
                if (p1.Contains(u.Id + ""))
                    return new string[] { u.Id + "", p2 };
            }

            if (p1IsName) return new string[] { id + "", p2 };
            else return new string[] { GetLastMessage(c, id).User.Id + "", p1 };
        }

        private bool ValidateParameters(string[] paramValues) {
            //If invalid parameters
            if (paramValues == null) return false;

            //If rank is out of range
            int rank = 0;
            int.TryParse(paramValues[1], out rank);
            if (rank > 5 || rank < 0) return false;

            //If params are ok
            return true;
        }

        private string GetAttachment(Message m) {
            if (m.Attachments.Length > 0) {
                return m.Attachments[0].Url;
            } else if (m.Text.Contains("http://") || m.Text.Contains("https://")) {
                string t = m.RawText;
                while (t.Contains(" ")) {
                    t = t.TrimEnd(' ');
                }
                return t;
            } else {
                return null;
            }
        }

        private Message GetLastMessage(Channel c, ulong id) {
            foreach (Message m in GetMessageList(c)) {
                if (m.Attachments.Length > 0 || m.Text.Contains("http://") || m.Text.Contains("https://")) {
                    if((m.User.Id == id || id == 0) && GetLastMessage(c).User.Id != id) return m;
                }

            }
            return null;

        }

        private Message[] GetMessageList(Channel c) {
            return c.DownloadMessages(100).Result;
        }

        private Message GetLastMessage(Channel c) {
            return c.DownloadMessages(1).Result[0];
        }

        private string GetMessageForRank(string[] paramValues, Channel c, ulong senderId, bool passNoMatterWhatCusImDoneWithThisBot) {

            if (!passNoMatterWhatCusImDoneWithThisBot) {
                if (!ValidateParameters(paramValues)) return "Invalid parameters";
                if (NewVote(paramValues[0], GetAttachment(GetLastMessage(c, ulong.Parse(paramValues[0]))))) return "You have already casted you vote for this meme.";
            }
            MySqlParameter[] parameters = new MySqlParameter[4];

            SqlQuery sqlQuery = new SqlQuery("localhost", "root", "[poassword", "memerank");

            parameters[0] = new MySqlParameter("?Meme_ID", MySqlDbType.Int64);
            parameters[0].Value = Int64.Parse(paramValues[0]);
            parameters[1] = new MySqlParameter("?Rank", MySqlDbType.Double);
            parameters[1].Value = Double.Parse(paramValues[1]);
            parameters[2] = new MySqlParameter("?Link", MySqlDbType.VarChar);
            if (!passNoMatterWhatCusImDoneWithThisBot) {
                parameters[2].Value = GetAttachment(GetLastMessage(c, ulong.Parse(paramValues[0])));
            } else {
                parameters[2].Value = "setup";
            }
            parameters[3] = new MySqlParameter("?ID", MySqlDbType.Int64);
            parameters[3].Value = Int64.Parse(senderId + "");

            sqlQuery.SetQuery("INSERT INTO Ranks (meme_id, rank, link, id) VALUES (?Meme_ID, ?Rank, ?Link, ?ID)", parameters);
            sqlQuery.ExecuteQuery(QueryTypes.INSERT);

            return "Rank added";
        }

        private bool NewVote(string id, string url) {
            MySqlParameter[] parameters = new MySqlParameter[1];
            SqlQuery sqlQuery = new SqlQuery("localhost", "root", "[poassword", "memerank");
            parameters[0] = new MySqlParameter("?ID", MySqlDbType.Int64);
            parameters[0].Value = Int64.Parse(id);
            sqlQuery.SetQuery("SELECT * FROM ranks WHERE id = ?ID", parameters);
            Dictionary<string, List<string>> results = sqlQuery.ExecuteQuery(QueryTypes.SELECT);

            List<string> links = new List<string>();
            results.TryGetValue("Links", out links);

            return links.Contains(url);
        }

        private string GetUserRank(ulong userId, string name) {
            MySqlParameter[] parameters = new MySqlParameter[1];
            SqlQuery sqlQuery = new SqlQuery("localhost", "root", "[poassword", "memerank");
            parameters[0] = new MySqlParameter("?meme_ID", MySqlDbType.Int64);
            parameters[0].Value = (Int64)userId;
            sqlQuery.SetQuery("SELECT * FROM ranks WHERE meme_id = ?meme_ID", parameters);
            Dictionary<string, List<string>> results = sqlQuery.ExecuteQuery(QueryTypes.SELECT);

            //Get ranks from sql
            List<string> ranks = new List<string>();
            results.TryGetValue("Ranks", out ranks);
            //Vars for average
            int count = 0;
            int total = 0;

            //Average
            foreach (string i in ranks) {
                count++;
                total += int.Parse(i);
            }
            if (count == 0) return name + " is not a user";
            else            return name + "'s rank is " + (double)total / count;
        }

        private void Log(object sender, LogMessageEventArgs e) {
            Console.WriteLine(e.Message);
        }

    }

}