using System;
using System.Data;
using System.Data.SqlClient;

namespace Server
{
    internal static class Db
    {
        private static string ConnStr
        {
            get
            {
                return System.Configuration.ConfigurationManager
                       .ConnectionStrings["DefaultConnection"].ConnectionString;
            }
        }

        public static bool UsernameExists(string email)
        {
            using (var cn = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand("SELECT 1 FROM dbo.Users WHERE Email=@e", cn))
            {
                cmd.Parameters.AddWithValue("@e", email);
                cn.Open();
                object o = cmd.ExecuteScalar();
                return o != null;
            }
        }

        public static void InsertUser(string firstname, string surname, DateTime? birthday,
                                      string gender, string email, string passwordHex)
        {
            using (var cn = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(@"INSERT INTO dbo.Users(Firstname,Surname,Birthday,Gender,Email,PasswordEncrypted) VALUES (@fn,@sn,@bd,@gd,@em,@ph)", cn))
            {
                cmd.Parameters.AddWithValue("@fn", firstname);
                cmd.Parameters.AddWithValue("@sn", surname);
                var pBd = cmd.Parameters.Add("@bd", SqlDbType.Date);
                pBd.Value = birthday.HasValue ? (object)birthday.Value : DBNull.Value;
                cmd.Parameters.AddWithValue("@gd", gender);
                cmd.Parameters.AddWithValue("@em", email);
                var p = cmd.Parameters.Add("@ph", SqlDbType.Char, 64);
                p.Value = passwordHex;

                cn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        public static (string Firstname, string Surname, DateTime? Birthday, string Email)? GetByEmail(string email)
        {
            using (var cn = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(
                "SELECT TOP 1 Firstname,Surname,Birthday,Email FROM dbo.Users WHERE Email=@e", cn))
            {
                cmd.Parameters.AddWithValue("@e", email);
                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    return (
                        rd["Firstname"].ToString(),
                        rd["Surname"].ToString(),
                        rd["Birthday"] == DBNull.Value ? (DateTime?)null : (DateTime)rd["Birthday"],
                        rd["Email"].ToString()
                    );
                }
            }
        }
        public static (string Firstname, string Surname, DateTime? Birthday, string Gender, string Email)?
            FindByLogin(string email, string passwordHex)
        {
            using (var cn = new SqlConnection(ConnStr))
            using (var cmd = new SqlCommand(@"SELECT Firstname,Surname,Birthday,Gender,Email FROM dbo.Users WHERE Email=@e AND PasswordEncrypted=@ph", cn))
            {
                cmd.Parameters.AddWithValue("@e", email);
                var p = cmd.Parameters.Add("@ph", SqlDbType.Char, 64);
                p.Value = passwordHex;

                cn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    string fn = rd.GetString(0);
                    string sn = rd.GetString(1);
                    DateTime? bd = rd.IsDBNull(2) ? (DateTime?)null : rd.GetDateTime(2);
                    string gd = rd.IsDBNull(3) ? "" : rd.GetString(3);
                    string em = rd.GetString(4);
                    return (fn, sn, bd, gd, em);
                }
            }
        }
    }
}
