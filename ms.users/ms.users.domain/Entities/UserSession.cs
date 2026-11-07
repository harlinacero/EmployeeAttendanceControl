namespace ms.users.domain.Entities
{
    /// <summary>
    /// A sign-in on one device, from login until it expires, is ended or is revoked.
    /// </summary>
    public class UserSession
    {
        public Guid SessionId { get; set; }
        public string UserName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset LastRenewedAt { get; set; }
        public DateTimeOffset? EndedAt { get; set; }
        public string EndReason { get; set; }

        public bool IsActive(DateTimeOffset now, TimeSpan idleLimit, TimeSpan absoluteLimit) =>
            EndedAt == null && now < CreatedAt + absoluteLimit && now < LastRenewedAt + idleLimit;

        /// <summary>
        /// Time until the absolute limit, used as time to live of the stored rows. At least one minute, so a write never fails for it.
        /// </summary>
        public TimeSpan RemainingLifetime(DateTimeOffset now, TimeSpan absoluteLimit)
        {
            var remaining = CreatedAt + absoluteLimit - now;
            return remaining > TimeSpan.FromMinutes(1) ? remaining : TimeSpan.FromMinutes(1);
        }
    }
}
