using System.Data.Entity;

namespace CommonLibrary
{
    public sealed class JobsViewDbContext : DbContext
    {
        public enum Deploy:byte {Local,Cloud };
        JobsViewDbContext() : base(getConnectionString(Deploy.Local))
        {

        }

        public static string getStorageConnectionString(Deploy type)
        {
            string conn = "";
            if (type == Deploy.Local)
            {
                conn = "UseDevelopmentStorage=true";
            }
            else if (type == Deploy.Cloud)
            {
                conn = "";
            }
            return conn;
        }

        public static string getConnectionString(Deploy type)
        {
            string conn = "";
            if(type==Deploy.Local)
            {
                conn = "Data Source=.; Initial Catalog=Test; User Id=sa; Password=";
            } else if (type == Deploy.Cloud)
            {
                conn = "";
            }
            return conn;
        }
        public static JobsViewDbContext Instance
        {
            get
            {
                return Nested.instance;
            }
        }

        class Nested
        {
            static Nested()
            {
            }

            internal static readonly JobsViewDbContext instance = new JobsViewDbContext();
        }

        public System.Data.Entity.DbSet<category> Categories { get; set; }
        public System.Data.Entity.DbSet<document> Documents { get; set; }
        public System.Data.Entity.DbSet<keyword> Keywords { get; set; }
        public System.Data.Entity.DbSet<doc_keyword> Document_Keyword { get; set; }
        public System.Data.Entity.DbSet<user_preference> User_Preference { get; set; }
    }
}
