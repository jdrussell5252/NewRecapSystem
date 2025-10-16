namespace NewRecap.MyAppHelper
{
    public class AppHelper
    {
        public static string GeneratePasswordHash(string password)
        {
            string passwordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(password);
            return passwordHash;
        }//End of 'GeneratePasswordHash'.

        public static bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
        }//End of 'VerifyPassword'.
    }
}
