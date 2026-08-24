using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class ManualScore
{
    [Key]
    public int ManualScoreId { get; set; }

    public int AttemptId { get; set; }

    public int QuestionId { get; set; }

    [Column(TypeName = "decimal(6, 2)")]
    public decimal Score { get; set; }

    public int GradedByControlId { get; set; }

    public DateTime GradedAt { get; set; }

    [ForeignKey("AttemptId")]
    [InverseProperty("ManualScores")]
    public virtual ExamAttempt Attempt { get; set; } = null!;

    [ForeignKey("GradedByControlId")]
    [InverseProperty("ManualScores")]
    public virtual User GradedByControl { get; set; } = null!;

    [ForeignKey("QuestionId")]
    [InverseProperty("ManualScores")]
    public virtual Question Question { get; set; } = null!;
}
