using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class EmailLog
{
    [Key]
    public int EmailLogId { get; set; }

    public int UserId { get; set; }

    [StringLength(200)]
    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    public DateTime SentAt { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("EmailLogs")]
    public virtual User User { get; set; } = null!;
}
