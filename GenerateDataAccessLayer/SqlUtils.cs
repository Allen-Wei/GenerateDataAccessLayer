using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using Ysd.DataAccessLayer.Models;
using Dapper;

namespace Ysd.DataAccessLayer
{
    public static class SqlUtils
    {

        public static ModelContext GetContext()
        {
            return new ModelContext();
        }
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(GetContext().Connection.ConnectionString);
        }


        public static T GetValue<T>(string sql, object parameters)
        {
            return GetConnection().ExecuteScalar<T>(sql, parameters);
        }

        public static T GetValue<T>(string sql, object parameters, int time)
        {
            return GetConnection().ExecuteScalar<T>(sql: sql, param: parameters, commandTimeout: time);
        }


        public static T TryGetValue<T>(string sql, object parameters, T defaultWhenFail)
        {
            try
            {
                return GetConnection().ExecuteScalar<T>(sql, parameters);
            }
            catch
            {
                return defaultWhenFail;
            }
        }

        public static T TryGetValue<T>(string sql, object parameters, int time, T defaultWhenFail)
        {
            try
            {
                return GetValue<T>(sql, parameters, time);
            }
            catch
            {
                return defaultWhenFail;
            }
        }



        public static IEnumerable<T> Query<T>(
            string sql,
            object parameter = null,
            CommandType cmdType = CommandType.Text,
            int? timeOut = null,
            SqlTransaction transaction = null)
        {
            var result = GetConnection().Query<T>(
                sql: sql,
                param: parameter,
                commandType: cmdType,
                commandTimeout: timeOut,
                transaction: transaction
                );
            return result;
        }
        public static IEnumerable<T> Query<T>(
            string[] fields,
            string from,
            string where,
            string orderby,
            int skip,
            int take)
        {
            var templateSql = String.Format(@"
declare @skip int, @take int
set @skip = {0}
set @take = {1}
select {2} from 
(select row_number() over (order by {5}) as _SortNumber, {2} from {3} where {4}) as T
where _SortNumber between @skip and @skip + @take 
order by {5}
", skip, take, String.Join(",", fields), from, where, orderby);

            return Query<T>(templateSql);
        }


        public static int Execute(Func<SqlConnection, SqlTransaction, int> exexuteSqlCallback)
        {
            var effectedRows = -1;
            var connection = GetConnection();
            connection.Open();
            var transaction = connection.BeginTransaction();
            try
            {
                effectedRows = exexuteSqlCallback(connection, transaction);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
            }
            finally
            {
                connection.Close();
            }

            return effectedRows;
        }

        public static int Execute(string sql, object parameters)
        {
            return Execute((connection, transaction) =>
            {
                var rows = 0;
                rows += connection.Execute(sql, parameters, transaction);
                return rows;
            });
        }

        public static int Execute(string sql, int time, object parameters)
        {
            return Execute((connection, transaction) =>
            {
                var rows = 0;
                rows += connection.Execute(
                    sql: sql,
                    param: parameters,
                    transaction: transaction,
                    commandTimeout: time);
                return rows;
            });
        }
        public static int Execute(List<string> sql)
        {
            return Execute((connection, transaction) =>
            {
                var rows = 0;
                sql.ForEach(s =>
                {
                    rows += connection.Execute(s, transaction);
                });
                return rows;
            });
        }
        public static int Execute(string sql, Dapper.DynamicParameters parameter, CommandType cmdType)
        {
            return Execute((connection, transaction) =>
            {
                var rows = 0;
                rows = connection.Execute(
                    sql: sql,
                    param: parameter,
                    commandType: cmdType,
                    transaction: transaction);
                return rows;
            });
        }

        public static IDataReader Read(
            string sql,
            object parameters = null,
            CommandType cmdType = CommandType.Text,
            int? cmdTimeout = null)
        {
            return GetConnection().ExecuteReader(
                sql: sql,
                param: parameters,
                commandType: cmdType,
                commandTimeout: cmdTimeout);
        }


        public static bool IsColumnExist(string table, string column)
        {
            return TryGetValue<int>(
                "select count(*) from INFORMATION_SCHEMA.COLUMNS where TABLE_NAME=@Table and COLUMN_NAME=@Column",
                new { Table = table, Column = column },
                -1) == 1;
        }
        public static int GetMaxId(string table, string column)
        {
            return TryGetValue<int>("select max(@column) + 1 from [table]".Replace("[table]", table), new { column }, 1);
        }

        public static int GetRecordCount(string table)
        {
            return TryGetValue<int>(String.Format("select count(*) from {0}", table), null, -1);
        }
        public static bool IsExist(string sql, object parameters)
        {
            var value = TryGetValue<object>(sql, parameters, null);

            return value != null && value != DBNull.Value;
        }




    }

}
