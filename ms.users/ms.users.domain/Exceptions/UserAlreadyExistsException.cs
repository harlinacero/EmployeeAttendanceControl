namespace ms.users.domain.Exceptions
{
    public class UserAlreadyExistsException : Exception
    {
        public UserAlreadyExistsException(string userName) : base($"User {userName} already exists")
        {
        }
    }
}
