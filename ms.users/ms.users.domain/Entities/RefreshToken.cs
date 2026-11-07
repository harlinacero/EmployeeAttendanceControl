namespace ms.users.domain.Entities
{
    /// <summary>
    /// A single-use renewal credential. Only its hash is stored.
    /// </summary>
    public class RefreshToken
    {
        public string TokenHash { get; set; }
        public Guid SessionId { get; set; }
        public DateTimeOffset? SpentAt { get; set; }
    }
}
