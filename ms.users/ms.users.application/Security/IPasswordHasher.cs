namespace ms.users.application.Security
{
    public interface IPasswordHasher
    {
        string Hash(string password);

        bool Verify(string password, string storedValue);

        /// <summary>
        /// True only for a value in the complete protected format.
        /// </summary>
        bool IsProtected(string storedValue);
    }
}
