namespace TaskFlow.Core.Models
{
    public static class CurrentUser
    {
        public static User? User { get; private set; }

        public static bool IsLoggedIn => User is not null;

        public static bool IsAdmin => User?.Role?.ToLower() switch
        {
            "председатель" => true,
            "admin" => true,
            "администратор" => true,
            _ => false
        };

        public static void Login(User user)
        {
            User = user;
        }

        public static void Logout()
        {
            User = null;
        }
    }
}
