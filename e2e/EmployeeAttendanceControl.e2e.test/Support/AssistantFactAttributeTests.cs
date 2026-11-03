namespace EmployeeAttendanceControl.e2e.test.Support
{
    public class AssistantFactAttributeTests
    {
        [Fact]
        public void SkipShouldNameMissingRequestWhenAssistantNotRequested()
        {
            // Act
            var reason = AssistantFactAttribute.SkipReason(requested: null, keyProvided: "true");

            // Assert
            Assert.Contains("not requested", reason);
        }

        [Fact]
        public void SkipShouldNameMissingKeyWhenKeyNotProvided()
        {
            // Act
            var reason = AssistantFactAttribute.SkipReason(requested: "true", keyProvided: null);

            // Assert
            Assert.Contains("ANTHROPIC_API_KEY not provided", reason);
        }

        [Fact]
        public void SkipShouldBeEmptyWhenRequestedWithKey()
        {
            // Act
            var reason = AssistantFactAttribute.SkipReason(requested: "true", keyProvided: "true");

            // Assert
            Assert.Null(reason);
        }
    }
}
