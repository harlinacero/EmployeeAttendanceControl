using EmployeeAttendanceControl.e2e.test.Support;

namespace EmployeeAttendanceControl.e2e.test.Flows
{
    [Collection(E2ECollection.Name)]
    public class AssistantTests
    {
        private readonly E2EEnvironmentFixture _environment;

        public AssistantTests(E2EEnvironmentFixture environment) => _environment = environment;

        [AssistantFact]
        public async Task ChatShouldReturnAnAnswer()
        {
            // Act
            var answer = await _environment.Client.ChatAsync(_environment.RegularEmployee.Token, "How many attendances do I have?");

            // Assert
            Assert.False(string.IsNullOrWhiteSpace(answer.Answer), "POST /Assistant/Chat as regular user: expected an answer, got an empty one");
        }
    }
}
