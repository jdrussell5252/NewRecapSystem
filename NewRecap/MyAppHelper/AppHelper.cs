namespace NewRecap.MyAppHelper
{
    public class AppHelper
    {

        public static string GetDBConnectionString()
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();

            var connStr = config.GetConnectionString("DefaultConnection");

            // Fallback to LocalDB if not running in Azure
            if (string.IsNullOrEmpty(connStr))
            {
                // Optional: log that it's using the local fallback
                connStr = "Server=(localdb)\\MSSQLLocalDB;Database=NewRecapDB;Trusted_Connection=True";
            }

            return connStr;
        }// End of 'GetDBConnectionString'.

        public static string GeneratePasswordHash(string password)
        {
            string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password);
            return passwordHash;
        }//End of 'GeneratePasswordHash'.

        public static bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
        }//End of 'VerifyPassword'.
    }// End of 'AppHelper' Class.
}// End of 'namespace'.
