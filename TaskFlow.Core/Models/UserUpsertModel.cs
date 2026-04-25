using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace TaskFlow.Core.Models
{
    [Table("users")]
    public class UserUpsertModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Column("role")]
        public string Role { get; set; } = string.Empty;

        [Column("email")]
        public string? Email { get; set; }

        [Column("phone")]
        public string? Phone { get; set; }

        [Column("password")]
        public string Password { get; set; } = string.Empty;

        [Column("login")]
        public string Login { get; set; } = string.Empty;

        [Column("is_blocked")]
        public bool IsBlocked { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
