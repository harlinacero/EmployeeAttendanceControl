using ms.assistant.application.HttpComunications;
using Refit;

namespace ms.assistant.application.test.HttpComunications
{
    public class HttpComunicationsTests
    {
        [Fact]
        public void InterfacesShouldOnlyDeclareGetOperations()
        {
            // Arrange
            var interfaces = typeof(IAttendanceApiCommunication).Assembly.GetTypes()
                .Where(type => type.IsInterface && type.Namespace == typeof(IAttendanceApiCommunication).Namespace)
                .ToList();

            // Act
            var methods = interfaces.SelectMany(type => type.GetMethods()).ToList();

            // Assert
            Assert.NotEmpty(methods);
            Assert.All(methods, method =>
            {
                Assert.True(method.IsDefined(typeof(GetAttribute), inherit: false), $"{method.DeclaringType!.Name}.{method.Name} is not a GET.");
                Assert.False(method.IsDefined(typeof(PostAttribute), inherit: false));
                Assert.False(method.IsDefined(typeof(PutAttribute), inherit: false));
                Assert.False(method.IsDefined(typeof(DeleteAttribute), inherit: false));
                Assert.False(method.IsDefined(typeof(PatchAttribute), inherit: false));
            });
        }
    }
}
