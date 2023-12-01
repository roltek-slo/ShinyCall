using Dapper;
using ShinyCall.Mappings;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShinyCall.Sqlite
{
    public class SqliteDataAccess
    {

        public static List<CallModel> LoadCalls() {

            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<CallModel>("select * from CallHistory", new DynamicParameters());
                return output.ToList(); 
            }
        }


        public static List<ContactsModel> LoadDataContacts() { 
        
            using(IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<ContactsModel>("select * from Contacts", new DynamicParameters());
                return output.ToList();
            }        
        
        }

        public static List<LinksModel> LoadDataLinks()
        {

            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<LinksModel>("select * from Links", new DynamicParameters());
                return output.ToList();
            }

        }

        internal static void DeleteContact(int phone)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("delete from Contacts where phone=@phone", new ContactsModel { phone = phone});
            }
        }

        public static void InsertLinks(LinksModel model)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("insert into Links(link, desc) values (@link, @desc)", model);
            }
        }

        internal static void DeleteLink(LinksModel link)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("delete from Links where desc = @desc", link);
            }
        }

        internal static void DeleteHistory()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("delete from CallHistory;", new DynamicParameters());
            }

        }

        public static void InsertContacts(ContactsModel model)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("insert into Contacts(phone, name) values (@phone, @name)", model);
       
                
            }
        }

        public static void InsertCallHistory(CallModel model)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("insert into CallHistory(caller, status, time) values (@caller, @status, @time)", model);
            }
        }


        public static void UpdateCallHistory(CallModel model)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                cnn.Execute("update CallHistory set caller = @caller, status = @status where time = @time;", model);
            }
        }

        public static LinksModel? GetLinkBasedOnName(LinksModel model)
        {
            LinksModel link = null;
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.QuerySingle<LinksModel>("select * from Links where desc=@desc", model);
                link = (LinksModel) output;
            }
            return link;
        }
        public static ContactsModel? GetContact(ContactsModel model)
        {
            ContactsModel contact = null;
            
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.QuerySingle<ContactsModel>("select * from Contacts where phone=@phone", model);
                contact = (ContactsModel) output;
                return contact;
            }

        } 

        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
