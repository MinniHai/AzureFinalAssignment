using System.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public sealed class JobsViewDbContext : DbContext
    {
        public enum Deploy:byte {Local,Cloud };

        private const Deploy type = Deploy.Local;
        JobsViewDbContext() : base(getConnectionString())
        {

        }

        public static string getStorageConnectionString()
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

        public static string getConnectionString()
        {
            string conn = "";
            if(type==Deploy.Local)
            {
                conn = @"Data Source=.; Initial Catalog=TEST; User Id=sa; Password=;";
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
        public System.Data.Entity.DbSet<ad> Ads { get; set; }
    }
}
