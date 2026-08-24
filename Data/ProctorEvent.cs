using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniRemoteExam.Data;

public partial class ProctorEvent
{
    [Key]
    public int ProctorEventId { get; set; }

    public int AttemptId { get; set; }

    public int? QuestionId { get; set; }

    public int StudentId { get; set; }

    [StringLength(80)]
    public string EventType { get; set; } = null!;

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("AttemptId")]
    [InverseProperty("ProctorEvents")]
    public virtual ExamAttempt Attempt { get; set; } = null!;

    [ForeignKey("QuestionId")]
    [InverseProperty("ProctorEvents")]
    public virtual Question? Question { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("ProctorEvents")]
    public virtual User Student { get; set; } = null!;
}
