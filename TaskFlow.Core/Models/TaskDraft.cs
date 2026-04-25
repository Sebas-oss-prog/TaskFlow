using System;

namespace TaskFlow.Core.Models
{
    public class TaskDraft
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Guid? ResponsibleId { get; set; }

        public DateTime? DueDate { get; set; }

        public string Priority { get; set; } = "Средний";

        public string Status { get; set; } = "Новая";
    }
}
