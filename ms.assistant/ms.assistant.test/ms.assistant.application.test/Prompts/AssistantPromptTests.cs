using ms.assistant.application.Prompts;

namespace ms.assistant.application.test.Prompts
{
    public class AssistantPromptTests
    {
        private static readonly DateOnly Today = new(2026, 10, 29);

        [Fact]
        public void BuildShouldIncludeTodayDate()
        {
            // Act
            var prompt = AssistantPrompt.Build(isAdmin: false, Today);

            // Assert
            Assert.Contains("2026-10-29", prompt);
        }

        [Fact]
        public void BuildShouldRestrictEmployeeToOwnAttendance()
        {
            // Act
            var employeePrompt = AssistantPrompt.Build(isAdmin: false, Today);
            var adminPrompt = AssistantPrompt.Build(isAdmin: true, Today);

            // Assert
            Assert.Contains(AssistantPrompt.OwnDataOnlyRule, employeePrompt);
            Assert.DoesNotContain(AssistantPrompt.OwnDataOnlyRule, adminPrompt);
        }

        [Fact]
        public void BuildShouldStateAdminRangeLimit()
        {
            // Act
            var adminPrompt = AssistantPrompt.Build(isAdmin: true, Today);

            // Assert
            Assert.Contains("31 days", adminPrompt);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BuildShouldStateToolResultsAreDataNotInstructions(bool isAdmin)
        {
            // Act
            var prompt = AssistantPrompt.Build(isAdmin, Today);

            // Assert
            Assert.Contains(AssistantPrompt.ToolResultsAreDataRule, prompt);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BuildShouldStateReadOnlyScopeLanguageAndNoInvention(bool isAdmin)
        {
            // Act
            var prompt = AssistantPrompt.Build(isAdmin, Today);

            // Assert
            Assert.Contains(AssistantPrompt.ReadOnlyRule, prompt);
            Assert.Contains(AssistantPrompt.ScopeRule, prompt);
            Assert.Contains(AssistantPrompt.LanguageRule, prompt);
            Assert.Contains(AssistantPrompt.NoInventionRule, prompt);
            Assert.Contains(AssistantPrompt.ClarifyRule, prompt);
        }
    }
}
