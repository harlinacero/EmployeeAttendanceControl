namespace ms.assistant.application.Tools
{
    public interface IAssistantToolCatalog
    {
        IReadOnlyList<IAssistantTool> For(bool isAdmin);
    }
}
