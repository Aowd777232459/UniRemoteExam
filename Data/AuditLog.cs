using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class AuditLog
{
    [Key]
    public int AuditId { get; set; }

    public int? ActorUserId { get; set; }

    [StringLength(100)]
    public string Action { get; set; } = null!;

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("ActorUserId")]
    [InverseProperty("AuditLogs")]
    public virtual User? ActorUser { get; set; }
}
