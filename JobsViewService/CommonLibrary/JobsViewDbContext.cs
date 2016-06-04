using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary
{
    public sealed class JobsViewDbContext : DbContext
    {
        JobsViewDbContext() : base("name=JobsViewDbContext")
        {

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

        public System.Data.Entity.DbSet<Category> Categories { get; set; }
        public System.Data.Entity.DbSet<Document> Documents { get; set; }
        public System.Data.Entity.DbSet<Keyword> Keywords { get; set; }
        public System.Data.Entity.DbSet<Document_Keyword> Document_Keyword { get; set; }
        public System.Data.Entity.DbSet<User_Preference> User_Preference { get; set; }
    }
}
