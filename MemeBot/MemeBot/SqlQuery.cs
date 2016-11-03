﻿using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;

namespace MemeBot {

    public enum QueryTypes {
        INSERT, SELECT
    }

    class SqlQuery {

        private string queryString;
        private MySqlParameter[] parameters;

        //Login infomation
        private string server;
        private string user;
        private string password;
        private string database;

        internal SqlQuery(string server, string user, string password, string database) {
            this.server = server;
            this.user = user;
            this.password = password;
            this.database = database;
        }

        internal void SetQuery(string query, MySqlParameter[] parameters) {
            this.queryString = query;
            this.parameters = parameters;
        }

        internal Dictionary<string, List<string>> ExecuteQuery(QueryTypes type) {
            MySqlDataReader dataReader = null;

            List<string> ids = new List<string>();
            List<string> ranks = new List<string>();
            List<string> links = new List<string>();


            using (MySqlConnection conn = new MySqlConnection("Server=" + server + ";Uid=" + user + ";Pwd=" + password + ";Database=" + database + ";")) {
                using (MySqlCommand command = new MySqlCommand(queryString, conn)) {
                    try {
                        //Add parameters to query
                        if (parameters != null) foreach (MySqlParameter p in parameters) command.Parameters.Add(p);
                        conn.Open();
                        switch (type) {
                            case QueryTypes.INSERT: { command.ExecuteNonQuery(); break; }
                            case QueryTypes.SELECT: { dataReader = command.ExecuteReader(); break; }
                            default: { throw new InvalidOperationException(); }
                        }
                        
                        while(dataReader.Read()) {
                            ids.Add(dataReader.GetInt64(0) + "");
                            ranks.Add(dataReader.GetDouble(1) + "");
                            links.Add(dataReader.GetString(2));
                        }

                    } catch (SqlException e) {
                        Console.WriteLine(e);
                    } finally {
                        command.Clone();
                        conn.Clone();
                    }

                }

            }
            Dictionary<string, List<string>> results = new Dictionary<string, List<string>>();
            results.Add("Ids", ids);
            results.Add("Ranks", ranks);
            results.Add("Links", links);
            return results;
        }

    }

}