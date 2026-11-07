using ms.users.domain.Entities;

namespace ms.users.application.Security
{
    public interface IAccessTokenIssuer
    {
        int LifetimeSeconds { get; }

        string Issue(User user);
    }
}
