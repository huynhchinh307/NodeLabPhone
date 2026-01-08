using System.IO;

namespace NodeLabFarm.Services
{
    public interface IAuthService
    {
        bool IsLoggedIn();
        void Login();
        void Logout();
    }

    public class AuthService : IAuthService
    {
        private const string SessionFile = "session.dat";

        public bool IsLoggedIn()
        {
            return File.Exists(SessionFile);
        }

        public void Login()
        {
            File.WriteAllText(SessionFile, "logged_in");
        }

        public void Logout()
        {
            if (File.Exists(SessionFile))
            {
                File.Delete(SessionFile);
            }
        }
    }
}
