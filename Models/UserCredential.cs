namespace UnitPlanGenerator.Models
{
    public enum DatabaseProvider
    {
        SQLite,
        PostgreSQL,
    }

    public class UserCredential
    {
        public DatabaseProvider DatabaseProvider { get; set; }

        public string ConnectionString { get; set; }

        public string UserName { get; set; }
    }
}
