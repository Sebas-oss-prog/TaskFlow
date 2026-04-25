using System;

namespace TaskFlow.Core.Models
{
    public class TaskTemplate
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Priority { get; set; } = "Средний";

        public int DefaultDueInDays { get; set; } = 3;

        public string AccentColor { get; set; } = "#3F51B5";
    }
}
