namespace T.Data
{
    using System.Data;

    using ServiceStack.OrmLite;

    public static class DataAccessLayer
    {
        public static IDbConnection CreateFactory(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return null;

            var factory = new OrmLiteConnectionFactory(connectionString, MySqlDialect.Provider);
            return factory.Open();
        }
    }
}