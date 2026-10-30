namespace ms.assistant.application.Tools
{
    public class AssistantToolCatalog : IAssistantToolCatalog
    {
        private readonly IReadOnlyList<IAssistantTool> _tools;

        public AssistantToolCatalog(IEnumerable<IAssistantTool> tools) => _tools = tools.ToList();

        public IReadOnlyList<IAssistantTool> For(bool isAdmin)
            => _tools.Where(tool => isAdmin || !tool.AdminOnly).ToList();
    }
}
