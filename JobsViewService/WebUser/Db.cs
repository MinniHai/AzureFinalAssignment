using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Globalization;
using System.Linq;
using CommonLibrary;

namespace WebUser
{
    public sealed class Db
    {
        

        private string connectionString = null;
        public Db() : base()
        {
            connectionString = JobsViewDbContext.getConnectionString(JobsViewDbContext.Deploy.Local); 

        }


        public static Db Instance
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

            internal static Db instance = new Db();
        }

        public IEnumerable<string> getUserReferenceByUserId(string id)
        {
            // Create SQL connection
            ////var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var connection = new SqlConnection();


            try
            {
                connection.ConnectionString = connectionString;

                var command = new SqlCommand("SELECT up.Preferences FROM user_preference up WHERE UserId=@p1")
                {
                    Connection = connection
                };

                var p1 = new SqlParameter("p1", id);
                command.Parameters.Add(p1);

                connection.Open();

                var dataAdapter = new SqlDataAdapter(command);

                var ds = new DataSet();

                dataAdapter.Fill(ds, "user_preference");

                var listPreference = new List<string>();

                string preference = null;

                foreach (DataRow row in ds.Tables["user_preference"].Rows)
                {
                    preference = row["Preferences"].ToString();

                }
                if (preference == null) return listPreference;
                var words = preference.Split(',');
                listPreference.AddRange(words);
                return listPreference;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            return null;
        }

        public document getDocumentByID(int docId)
        {
            // Create SQL connection
            //var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var connection = new SqlConnection();
            try
            {
                connection.ConnectionString = connectionString;

                var command = new SqlCommand("SELECT * FROM document WHERE DocumentId=@p1") { Connection = connection };

                var p1 = new SqlParameter("p1", docId);
                command.Parameters.Add(p1);

                connection.Open();

                var dataAdapter = new SqlDataAdapter(command);

                var ds = new DataSet();

                dataAdapter.Fill(ds, "document");

                var doc = new document();

                foreach (DataRow row in ds.Tables["document"].Rows)
                {
                    doc.DocumentId = int.Parse(row["DocumentId"].ToString());
                    doc.Title = row["Title"].ToString();
                    doc.CategoryId = int.Parse(row["CategoryId"].ToString());
                    doc.Text = row["Text"].ToString();
                    doc.Html = row["Html"].ToString();
                    doc.Link = row["Link"].ToString();
                }
                return doc;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            return null;
        }

        public List<int> getListDocIDnonDoc(int docId)
        {
            // Create SQL connection
            //var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var connection = new SqlConnection();
            try
            {
                connection.ConnectionString = connectionString;

                var command = new SqlCommand("SELECT * FROM document WHERE DocumentId!=@p1") { Connection = connection };

                var parameter = new SqlParameter("p1", docId);
                command.Parameters.Add(parameter);

                connection.Open();

                var dataAdapter = new SqlDataAdapter(command);

                var ds = new DataSet();

                dataAdapter.Fill(ds, "document");

                return (from DataRow row in ds.Tables["document"].Rows select int.Parse(row["DocumentId"].ToString())).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            return null;
        }

        public List<float> GetTfidf(int docId)
        {
            // Create SQL connection
            //var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var connection = new SqlConnection();

            try
            {
                connection.ConnectionString = connectionString;

                var command = new SqlCommand("SELECT dk.TFIDF FROM doc_keyword dk WHERE dk.DocumentId=@p1")
                {
                    Connection = connection
                };

                var p1 = new SqlParameter("p1", docId);
                command.Parameters.Add(p1);

                connection.Open();

                var dataAdapter = new SqlDataAdapter(command);

                var ds = new DataSet();

                dataAdapter.Fill(ds, "doc_keyword");

                var tfidfList =
                    (from DataRow row in ds.Tables["doc_keyword"].Rows where true select float.Parse(row["TFIDF"].ToString(), CultureInfo.InvariantCulture)).ToList();

                connection.Close();
                return tfidfList;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            return null;
        }

        public List<DocKeyword> GetlistdocKeyword()
        {

            //var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var builder = new SqlConnectionStringBuilder(connectionString);
            var connection = new SqlConnection(builder.ConnectionString);

            var docKeywordList = new List<DocKeyword>();

            try
            {
                connection.Open();
                const string sql = "SELECT dk.DocumentId, k.keyword FROM doc_keyword dk, keyword k WHERE dk.KeywordId=k.DocumentId AND dk.TF != 0 AND dk.TFIDF != 0";
                var command = new SqlCommand(sql, connection);

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var record = (IDataRecord)reader;
                    var docKeyword = new DocKeyword
                    {
                        DocId = Int32.Parse(record["DocumentId"].ToString()),
                        Keyword = record["keyword"].ToString()
                    };

                    docKeywordList.Add(docKeyword);
                }
                return docKeywordList;
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            return null;
        }

        public List<int> GetlistUserId()
        {

            //var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var builder = new SqlConnectionStringBuilder(connectionString);
            var connection = new SqlConnection(builder.ConnectionString);

            var userIdList = new List<int>();

            try
            {
                connection.Open();
                const string sql = "SELECT up.UserId FROM user_preference up";
                var command = new SqlCommand(sql, connection);

                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var record = (IDataRecord)reader;
                    userIdList.Add(Int32.Parse(record["UserId"].ToString()));
                }
                return userIdList;
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            return null;
        }

        public List<ads> GetAllAds()
        {
            // Create SQL connection
            //var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var connection = new SqlConnection();
            try
            {
                connection.ConnectionString = connectionString;

                var command = new SqlCommand("SELECT * FROM ads");
                command.Connection = connection;

                connection.Open();

                var dataAdapter = new SqlDataAdapter(command);

                var ds = new DataSet();

                dataAdapter.Fill(ds, "ads");

                return (from DataRow row in ds.Tables["ads"].Rows
                        select new ads
                        {
                            AdId = int.Parse(row["AdId"].ToString()),
                            Title = row["Title"].ToString(),
                            Price = int.Parse(row["Price"].ToString()),
                            Description = row["Description"].ToString(),
                            ImageURL = row["ImageURL"].ToString(),
                            ThumbnailURL = row["ThumbnailURL"].ToString(),
                            PostedDate = DateTime.Parse(row["PostedDate"].ToString()),
                            CategoryId = int.Parse(row["CategoryId"].ToString()),
                            Phone = row["Phone"].ToString(),
                            Keyword = row["keyword"].ToString()
                        }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            return null;
        }
        public List<DocKeyword> GetlistdocKeywordByDocId(int DocId)
        {

            //var connectionString = ConfigurationManager.AppSettings["ConnectionString"];
            var builder = new SqlConnectionStringBuilder(connectionString);
            var connection = new SqlConnection(builder.ConnectionString);

            var docKeywordList = new List<DocKeyword>();

            try
            {
                connection.Open();
                const string sql = "SELECT dk.DocumentId, k.keyword FROM doc_keyword dk, keyword k WHERE dk.KeywordId=k.DocumentId AND dk.TF != 0 AND dk.TFIDF != 0 AND DocumentId=@p1";
                var command = new SqlCommand(sql, connection);
                var parameter = new SqlParameter("p1", DocId);
                command.Parameters.Add(parameter);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var record = (IDataRecord)reader;
                    var docKeyword = new DocKeyword
                    {
                        DocId = Int32.Parse(record["DocumentId"].ToString()),
                        Keyword = record["keyword"].ToString()
                    };

                    docKeywordList.Add(docKeyword);
                }
                return docKeywordList;
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            return null;
        }

    }
}