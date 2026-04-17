using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Text.Json.Serialization;

namespace TaskFlow.Models
{
    [Table("tasks")]
    public class TaskItem : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("responsible_id")]
        public Guid? ResponsibleId { get; set; }

        [Column("assigned_by_id")]
        public Guid? AssignedById { get; set; }

        [Column("due_date")]
        public DateTime? DueDate { get; set; }

        [Column("status")]
        public string Status { get; set; } = "Новая";

        [Column("priority")]
        public string Priority { get; set; } = "Средний";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Вспомогательные поля ТОЛЬКО для отображения в интерфейсе
        // Они полностью игнорируются Supabase
        [JsonIgnore]
        public string ResponsibleName { get; set; } = "Не назначен";

        [JsonIgnore]
        public string AssignedByName { get; set; } = "";
    }
}