namespace ms.users.domain.Exceptions
{
    /// <summary>
    /// The users' data can't be reached. Reported as temporarily unavailable, never as wrong credentials.
    /// </summary>
    public class DataUnavailableException : Exception
    {
        public DataUnavailableException(Exception innerException) : base("The users' data is temporarily unavailable", innerException)
        {
        }
    }
}
