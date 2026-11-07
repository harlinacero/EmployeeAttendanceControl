namespace ms.employees.domain.Exceptions
{
    /// <summary>
    /// The account couldn't be created in ms.users, so the employee isn't created either.
    /// </summary>
    public class UsersServiceUnavailableException : Exception
    {
        public UsersServiceUnavailableException(Exception innerException)
            : base("The users service is temporarily unavailable", innerException)
        {
        }
    }
}
