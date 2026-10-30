using Anthropic.Models.Messages;
using ms.assistant.domain.Entities;
using ms.assistant.infraestucture.Extensions;
using System.Text.Json;

namespace ms.assistant.infraestucture.AI
{
    /// <summary>
    /// Converts between the provider-neutral domain types and the Anthropic SDK types.
    /// </summary>
    public static class ClaudeMessageMapper
    {
        public static MessageCreateParams ToParams(AnthropicOptions options, string systemPrompt, IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition> tools)
            => new()
            {
                Model = options.Model,
                MaxTokens = options.MaxTokens,
                OutputConfig = new OutputConfig { Effort = ToEffort(options.Effort) },
                System = systemPrompt,
                Tools = tools.Select(ToTool).ToList(),
                Messages = messages.Select(ToMessageParam).ToList()
            };

        public static ModelReply ToReply(Message message)
        {
            var text = string.Concat(message.Content.Select(block => block.Value).OfType<TextBlock>().Select(block => block.Text));
            var toolCalls = message.Content.Select(block => block.Value).OfType<ToolUseBlock>()
                .Select(block => new ToolCall(block.ID, block.Name, JsonSerializer.SerializeToElement(block.Input)))
                .ToList();
            var providerContent = message.Content.Select(ToContentBlockParam).Where(block => block is not null).ToList();

            return new ModelReply(
                ToStopReason(message.StopReason?.Raw()),
                text,
                toolCalls,
                ChatMessage.AssistantToolCalls(text, toolCalls, providerContent),
                (int)message.Usage.InputTokens,
                (int)message.Usage.OutputTokens);
        }

        private static ToolUnion ToTool(ToolDefinition definition) => new Tool
        {
            Name = definition.Name,
            Description = definition.Description,
            InputSchema = new()
            {
                Properties = definition.InputSchema.ToDictionary(property => property.Key, property => property.Value),
                Required = definition.Required.ToList()
            }
        };

        private static MessageParam ToMessageParam(ChatMessage message)
        {
            if (message.ToolResults.Count > 0)
            {
                return new UserMessageParam
                {
                    Content = message.ToolResults
                        .Select(result => (ContentBlockParam)new ToolResultBlockParam { ToolUseID = result.ToolCallId, Content = result.Content, IsError = result.IsError })
                        .ToList()
                };
            }

            if (message.ProviderContent is List<ContentBlockParam> providerContent)
            {
                // Echo the model's own content unchanged (thinking blocks must keep their signature).
                return new AssistantMessageParam { Content = providerContent };
            }

            if (message.ToolCalls.Count > 0)
            {
                var blocks = new List<ContentBlockParam>();
                if (!string.IsNullOrEmpty(message.Text))
                {
                    blocks.Add(new TextBlockParam { Text = message.Text });
                }
                blocks.AddRange(message.ToolCalls.Select(call => (ContentBlockParam)new ToolUseBlockParam
                {
                    ID = call.Id,
                    Name = call.Name,
                    Input = call.Input.Deserialize<Dictionary<string, JsonElement>>() ?? []
                }));
                return new AssistantMessageParam { Content = blocks };
            }

            return message.Role == ChatRole.Assistant
                ? new AssistantMessageParam { Content = message.Text }
                : new UserMessageParam { Content = message.Text };
        }

        private static ContentBlockParam ToContentBlockParam(ContentBlock block)
        {
            if (block.TryPickText(out TextBlock text))
            {
                return new TextBlockParam { Text = text.Text };
            }
            if (block.TryPickThinking(out ThinkingBlock thinking))
            {
                return new ThinkingBlockParam { Thinking = thinking.Thinking, Signature = thinking.Signature };
            }
            if (block.TryPickRedactedThinking(out RedactedThinkingBlock redacted))
            {
                return new RedactedThinkingBlockParam { Data = redacted.Data };
            }
            if (block.TryPickToolUse(out ToolUseBlock toolUse))
            {
                return new ToolUseBlockParam { ID = toolUse.ID, Name = toolUse.Name, Input = toolUse.Input };
            }
            return null;
        }

        private static Effort ToEffort(string effort) => effort?.ToLowerInvariant() switch
        {
            "medium" => Effort.Medium,
            "high" => Effort.High,
            "max" => Effort.Max,
            _ => Effort.Low
        };

        private static ModelStopReason ToStopReason(string stopReason) => stopReason switch
        {
            "tool_use" => ModelStopReason.ToolUse,
            "max_tokens" => ModelStopReason.MaxTokens,
            "refusal" => ModelStopReason.Refusal,
            _ => ModelStopReason.EndTurn
        };
    }
}
