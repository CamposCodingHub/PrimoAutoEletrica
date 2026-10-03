using System;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Models.AI
{
    public enum AIRole
    {
        User,
        Assistant,
        System,
        Tool
    }

    public sealed class AISuggestedAction
    {
        public string Label { get; set; } = string.Empty;
        public string ActionType { get; set; } = "Navigate"; // "Navigate", "CopyText", "OpenDialog", "SearchStock"
        public string Parameter { get; set; } = string.Empty;
        public string? IconKey { get; set; }
    }

    public sealed class AIToolCall
    {
        public string ToolName { get; set; } = string.Empty;
        public Dictionary<string, string> Arguments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string? ExecutionResult { get; set; }
        public bool IsExecuted { get; set; }
        public bool Success { get; set; }
    }

    public sealed class AIChatMessage
    {
        public AIRole Role { get; set; } = AIRole.User;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? SenderName { get; set; }
        public List<AIToolCall> ToolCalls { get; set; } = new();
        public List<AISuggestedAction> SuggestedActions { get; set; } = new();
        public bool IsError { get; set; }

        public bool IsFromUser => Role == AIRole.User;
        public bool IsFromAssistant => Role == AIRole.Assistant;
        public bool HasActions => SuggestedActions != null && SuggestedActions.Count > 0;
    }

    public sealed class AIChatRequest
    {
        public List<AIChatMessage> Messages { get; set; } = new();
        public string? VehicleContext { get; set; }
        public string? WorkOrderContext { get; set; }
        public string? TechnicianContext { get; set; }
        public bool ForceOffline { get; set; }
    }

    public sealed class AIChatResponse
    {
        public string Message { get; set; } = string.Empty;
        public string ProviderUsed { get; set; } = "Offline Expert Engine";
        public List<AIToolCall> ToolCalls { get; set; } = new();
        public List<AISuggestedAction> SuggestedActions { get; set; } = new();
        public bool Success { get; set; } = true;
        public string? ErrorMessage { get; set; }
    }
}
