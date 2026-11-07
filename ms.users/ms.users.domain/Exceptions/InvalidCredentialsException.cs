namespace ms.users.domain.Exceptions
{
    /// <summary>
    /// Wrong, unknown, missing, spent or expired credentials. Every case gets the same answer.
    /// </summary>
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException() : base("Invalid credentials")
        {
        }
    }
}
