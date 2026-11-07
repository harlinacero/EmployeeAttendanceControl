using Microsoft.Extensions.Options;
using ms.users.application.Extensions;

namespace ms.users.application.test
{
    /// <summary>
    /// Settings with the production limits: access 15 minutes, session 7 days idle and 30 days at most.
    /// </summary>
    public static class TestSettings
    {
        public const string Issuer = "ms.users";

        public static IOptions<SettingsOptions> Create(string privateKeyPath = "unused.pem") => Options.Create(new SettingsOptions
        {
            Authentication = new Authentication
            {
                JWT = new JWT { Issuer = Issuer, PrivateKeyPath = privateKeyPath, AccessTokenMinutes = 15 },
                Session = new SessionLimits { IdleDays = 7, AbsoluteDays = 30 }
            }
        });
    }
}
