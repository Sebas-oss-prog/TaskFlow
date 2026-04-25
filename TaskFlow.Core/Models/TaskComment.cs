using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace TaskFlow.Core.Models
{
    [Table("task_comments")]
    public class TaskComment : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("task_id")]
        public Guid TaskId { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("comment")]
        public string Comment { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string UserName { get; set; } = string.Empty;
    }
}