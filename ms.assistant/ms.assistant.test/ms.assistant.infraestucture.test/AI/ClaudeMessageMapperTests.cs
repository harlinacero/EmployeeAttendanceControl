using Anthropic.Models.Messages;
using ms.assistant.domain.Entities;
using ms.assistant.infraestucture.AI;
using ms.assistant.infraestucture.Extensions;
using System.Text.Json;

namespace ms.assistant.infraestucture.test.AI
{
    public class ClaudeMessageMapperTests
    {
        private readonly AnthropicOptions _options = new() { Model = "claude-opus-5-5", Effort = "low", MaxTokens = 4096, ApiKey = "key" };

        private static ToolDefinition Tool(string name, params string[] required) => new(
            name,
            $"{name} description",
            required.ToDictionary(field => field, field => JsonSerializer.SerializeToElement(new { type = "string" })),
            required);

        private static string RoleOf(MessageParam message) =>
            message.TryPickUser(out _) ? "user" : message.TryPickAssistant(out _) ? "assistant" : "other";

        private static Message ParseMessage(string json) => JsonSerializer.Deserialize<Message>(json)!;

        [Fact]
        public void ToParamsShouldMapModelEffortMaxTokensSystemAndTools()
        {
            // Arrange
            var tools = new[] { Tool("get_my_attendances"), Tool("get_attendances_by_date_range", "userName", "from", "to") };

            // Act
            var parameters = ClaudeMessageMapper.ToParams(_options, "system prompt", [ChatMessage.User("hola")], tools);

            // Assert
            Assert.Equal("claude-opus-5-5", parameters.Model.Raw());
            Assert.Equal(4096, parameters.MaxTokens);
            Assert.Equal(Effort.Low, parameters.OutputConfig!.Effort!.Value());
            Assert.True(parameters.System!.TryPickString(out var system));
            Assert.Equal("system prompt", system);
            var mappedTools = parameters.Tools!.Select(tool => { Assert.True(tool.TryPickTool(out var t)); return t!; }).ToList();
            Assert.Equal(["get_my_attendances", "get_attendances_by_date_range"], mappedTools.Select(tool => tool.Name));
            Assert.Equal("get_attendances_by_date_range description", mappedTools[1].Description);
            Assert.Equal(["userName", "from", "to"], mappedTools[1].InputSchema.Required!);
            Assert.Equal(["userName", "from", "to"], mappedTools[1].InputSchema.Properties!.Keys);
        }

        [Fact]
        public void ToParamsShouldKeepMessageOrderAndRoles()
        {
            // Arrange
            var messages = new[] { ChatMessage.User("q1"), ChatMessage.Assistant("a1"), ChatMessage.User("q2") };

            // Act
            var parameters = ClaudeMessageMapper.ToParams(_options, "system", messages, []);

            // Assert
            Assert.Equal(["user", "assistant", "user"], parameters.Messages.Select(RoleOf));
            Assert.Equal(["q1", "a1", "q2"], parameters.Messages.Select(message =>
            {
                Assert.True(message.Content.TryPickString(out var text));
                return text;
            }));
        }

        [Fact]
        public void ToParamsShouldMapToolResultsWithIdsAndErrorFlag()
        {
            // Arrange
            var messages = new[]
            {
                ChatMessage.User("q"),
                ChatMessage.AssistantToolCalls("", [new ToolCall("t1", "get_my_attendances", JsonDocument.Parse("{}").RootElement)], providerContent: null),
                ChatMessage.Results([new ToolResult("t1", "x", true)])
            };

            // Act
            var parameters = ClaudeMessageMapper.ToParams(_options, "system", messages, []);

            // Assert
            Assert.True(parameters.Messages[1].Content.TryPickContentBlockParams(out var assistantBlocks));
            Assert.True(Assert.Single(assistantBlocks!).TryPickToolUse(out var toolUse));
            Assert.Equal("t1", toolUse!.ID);
            Assert.Equal("get_my_attendances", toolUse.Name);
            Assert.Equal("user", RoleOf(parameters.Messages[2]));
            Assert.True(parameters.Messages[2].Content.TryPickContentBlockParams(out var resultBlocks));
            Assert.True(Assert.Single(resultBlocks!).TryPickToolResult(out var toolResult));
            Assert.Equal("t1", toolResult!.ToolUseID);
            Assert.True(toolResult.IsError);
        }

        [Fact]
        public void ToParamsShouldEchoProviderContentUnchanged()
        {
            // Arrange
            var reply = ClaudeMessageMapper.ToReply(ParseMessage("""
                {"id":"msg_1","type":"message","role":"assistant","model":"claude-opus-5-5","stop_reason":"tool_use","stop_sequence":null,
                 "content":[{"type":"thinking","thinking":"","signature":"sig-123"},
                            {"type":"tool_use","id":"t1","name":"get_my_attendances","input":{}}],
                 "usage":{"input_tokens":10,"output_tokens":5}}
                """));

            // Act
            var parameters = ClaudeMessageMapper.ToParams(_options, "system", [ChatMessage.User("q"), reply.AssistantMessage], []);

            // Assert
            Assert.True(parameters.Messages[1].Content.TryPickContentBlockParams(out var blocks));
            Assert.Equal(2, blocks!.Count);
            Assert.True(blocks[0].TryPickThinking(out var thinking));
            Assert.Equal("sig-123", thinking!.Signature);
            Assert.True(blocks[1].TryPickToolUse(out var toolUse));
            Assert.Equal("t1", toolUse!.ID);
        }

        [Fact]
        public void ToReplyShouldMapTextToolCallsStopReasonAndUsage()
        {
            // Arrange
            var message = ParseMessage("""
                {"id":"msg_1","type":"message","role":"assistant","model":"claude-opus-5-5","stop_reason":"tool_use","stop_sequence":null,
                 "content":[{"type":"text","text":"Let me check."},
                            {"type":"tool_use","id":"t1","name":"get_attendances_by_date_range","input":{"userName":"jdoe","from":"2026-10-01","to":"2026-10-31"}}],
                 "usage":{"input_tokens":120,"output_tokens":30}}
                """);

            // Act
            var reply = ClaudeMessageMapper.ToReply(message);

            // Assert
            Assert.Equal(ModelStopReason.ToolUse, reply.StopReason);
            Assert.Equal("Let me check.", reply.Text);
            var call = Assert.Single(reply.ToolCalls);
            Assert.Equal("t1", call.Id);
            Assert.Equal("get_attendances_by_date_range", call.Name);
            Assert.Equal("jdoe", call.Input.GetProperty("userName").GetString());
            Assert.Equal(120, reply.InputTokens);
            Assert.Equal(30, reply.OutputTokens);
            Assert.Equal(ChatRole.Assistant, reply.AssistantMessage.Role);
            Assert.NotNull(reply.AssistantMessage.ProviderContent);
        }

        [Theory]
        [InlineData("end_turn", ModelStopReason.EndTurn)]
        [InlineData("refusal", ModelStopReason.Refusal)]
        [InlineData("max_tokens", ModelStopReason.MaxTokens)]
        public void ToReplyShouldMapStopReasons(string stopReason, ModelStopReason expected)
        {
            // Arrange
            var message = ParseMessage($$$"""
                {"id":"msg_1","type":"message","role":"assistant","model":"claude-opus-5-5","stop_reason":"{{{stopReason}}}","stop_sequence":null,
                 "content":[{"type":"text","text":"Hola"}],
                 "usage":{"input_tokens":1,"output_tokens":1}}
                """);

            // Act
            var reply = ClaudeMessageMapper.ToReply(message);

            // Assert
            Assert.Equal(expected, reply.StopReason);
        }
    }
}
