using System;

namespace TaskFlow.Core.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Text { get; set; } = string.Empty;

        public bool IsUser { get; set; }

        public bool CanCreateTask => !IsUser && !IsTransient;

        public bool IsTransient { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
