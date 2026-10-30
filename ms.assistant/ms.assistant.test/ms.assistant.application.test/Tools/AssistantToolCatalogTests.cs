using Moq;
using ms.assistant.application.Tools;

namespace ms.assistant.application.test.Tools
{
    public class AssistantToolCatalogTests
    {
        private readonly Mock<IAssistantTool> _employeeTool;
        private readonly Mock<IAssistantTool> _firstAdminTool;
        private readonly Mock<IAssistantTool> _secondAdminTool;

        public AssistantToolCatalogTests()
        {
            _employeeTool = CreateTool("get_my_attendances", adminOnly: false);
            _firstAdminTool = CreateTool("get_attendances_by_date_range", adminOnly: true);
            _secondAdminTool = CreateTool("list_employees", adminOnly: true);
        }

        private static Mock<IAssistantTool> CreateTool(string name, bool adminOnly)
        {
            var tool = new Mock<IAssistantTool>();
            tool.SetupGet(t => t.Name).Returns(name);
            tool.SetupGet(t => t.AdminOnly).Returns(adminOnly);
            return tool;
        }

        private AssistantToolCatalog CreateCatalog() =>
            new([_employeeTool.Object, _firstAdminTool.Object, _secondAdminTool.Object]);

        [Fact]
        public void ForShouldReturnOnlyNonAdminToolsWhenCallerIsNotAdmin()
        {
            // Arrange
            var catalog = CreateCatalog();

            // Act
            var tools = catalog.For(isAdmin: false);

            // Assert
            var tool = Assert.Single(tools);
            Assert.Same(_employeeTool.Object, tool);
        }

        [Fact]
        public void ForShouldReturnAllToolsWhenCallerIsAdmin()
        {
            // Arrange
            var catalog = CreateCatalog();

            // Act
            var tools = catalog.For(isAdmin: true);

            // Assert
            Assert.Equal(3, tools.Count);
            Assert.Contains(_firstAdminTool.Object, tools);
            Assert.Contains(_secondAdminTool.Object, tools);
        }
    }
}
