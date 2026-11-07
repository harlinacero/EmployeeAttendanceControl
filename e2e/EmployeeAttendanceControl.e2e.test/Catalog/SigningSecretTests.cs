using System.Text.RegularExpressions;

namespace EmployeeAttendanceControl.e2e.test.Catalog
{
    /// <summary>
    /// Stack-free: no service's configuration may hold anything that issues tokens. Only ms.users has a key, and it's generated, not configured.
    /// </summary>
    public class SigningSecretTests
    {
        private const string RetiredSharedSecret = "491a2a0099884cb79d2cd64018c910a5asdfasdfasdfasdfasdfasdfasdf";

        private static readonly Regex ConfiguredJwtKey = new(@"""JWT""\s*:\s*\{[^}]*""Key""|JWT:Key|JWT__Key", RegexOptions.IgnoreCase);

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "EmployeeAttendanceControl.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("Repository root not found above " + AppContext.BaseDirectory);
        }

        [Fact]
        public void NoServiceShouldHoldSigningSecret()
        {
            // Arrange
            var root = FindRepositoryRoot();
            var configurationFiles = Directory.EnumerateFiles(root, "appsettings*.json", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                    && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                .Concat(Directory.EnumerateFiles(root, "docker-compose*.yml"))
                .ToList();

            // Act
            var offending = configurationFiles
                .Where(path => File.ReadAllText(path) is var text && (text.Contains(RetiredSharedSecret) || ConfiguredJwtKey.IsMatch(text)))
                .Select(path => Path.GetRelativePath(root, path))
                .ToList();

            // Assert
            Assert.True(configurationFiles.Count >= 6, $"Expected the services' configuration files, found {configurationFiles.Count}");
            Assert.True(offending.Count == 0, "Files with a token signing secret: " + string.Join(", ", offending));
        }
    }
}
