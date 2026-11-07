using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ms.users.application.Extensions;
using System.Security.Cryptography;

namespace ms.users.application.Security
{
    /// <summary>
    /// Loads the private key from its PEM file, or generates and saves one when the file is missing.
    /// Replacing or deleting the file and restarting changes the key, and its key id with it.
    /// </summary>
    public class RsaSigningKeyProvider : ISigningKeyProvider
    {
        private const int KeySize = 2048;

        public RsaSecurityKey Key { get; }

        public RsaSigningKeyProvider(IOptions<SettingsOptions> settings, ILogger<RsaSigningKeyProvider> logger)
        {
            var path = Path.GetFullPath(settings.Value.Authentication.JWT.PrivateKeyPath);
            var rsa = RSA.Create(KeySize);
            var generated = !File.Exists(path);

            if (generated)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, rsa.ExportPkcs8PrivateKeyPem());
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
                }
            }
            else
            {
                rsa.ImportFromPem(File.ReadAllText(path));
            }

            Key = new RsaSecurityKey(rsa);
            Key.KeyId = Base64UrlEncoder.Encode(Key.ComputeJwkThumbprint());

            if (generated)
            {
                logger.LogInformation("Generated signing key {KeyId} at {KeyPath}", Key.KeyId, path);
            }
        }
    }
}
