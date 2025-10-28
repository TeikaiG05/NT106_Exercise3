namespace Common
{
    public static class MsgType
    {
        public const string REGISTER = "REGISTER";
        public const string LOGIN = "LOGIN";
        public const string LOGOUT = "LOGOUT";
        public const string LOGIN_WITH_TOKEN = "LOGIN_WITH_TOKEN";
    }

    public class TokenLoginReq
    {
        public string type { get; set; } = MsgType.LOGIN_WITH_TOKEN;
        public string username { get; set; }         // email
        public string token { get; set; }
    }

    public class RegisterReq
    {
        public string type { get; set; } = MsgType.REGISTER;
        public string username { get; set; }
        public string passwordHash { get; set; }
        public string email { get; set; }
        public string fullName { get; set; }
        public string birthday { get; set; }
    }

    public class LoginReq
    {
        public string type { get; set; } = MsgType.LOGIN;
        public string username { get; set; }
        public string passwordHash { get; set; }
    }

    public class UserDto
    {
        public string username { get; set; }
        public string email { get; set; }
        public string fullName { get; set; }
        public string birthday { get; set; }
    }

    public class OkRes
    {
        public bool ok { get; set; } = true;
        public string type { get; set; }
        public string message { get; set; }
        public UserDto user { get; set; }
        public string token { get; set; }
        public string expires { get; set; } //ISO8601
    }

    public class ErrRes
    {
        public bool ok { get; set; } = false;
        public string type { get; set; } = "ERROR";
        public string error { get; set; }
    }
  

    public class LogoutReq
    {
        public string type { get; set; } = MsgType.LOGOUT;
        public string username { get; set; }
        public string token { get; set; }
    }

}
