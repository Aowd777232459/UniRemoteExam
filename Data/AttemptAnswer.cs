using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class AttemptAnswer
{
    [Key]
    public int AttemptAnswerId { get; set; }

    public int AttemptId { get; set; }

    public int QuestionId { get; set; }

    public int? SelectedChoiceId { get; set; }

    public bool? BoolAnswer { get; set; }

    public string? EssayAnswer { get; set; }

    public bool Confirmed { get; set; }

    public DateTime? ConfirmedAt { get; set; }

    // وقت فتح السؤال للطالب. يستخدمه السيرفر لحساب انتهاء وقت السؤال.
    public DateTime? OpenedAt { get; set; }

    // وقت قفل السؤال، سواء بتأكيد الطالب أو بانتهاء الوقت.
    public DateTime? LockedAt { get; set; }

    // true إذا تم قفل السؤال بسبب انتهاء الوقت.
    public bool TimeExpired { get; set; }

    [ForeignKey("AttemptId")]
    [InverseProperty("AttemptAnswers")]
    public virtual ExamAttempt Attempt { get; set; } = null!;

    [ForeignKey("QuestionId")]
    [InverseProperty("AttemptAnswers")]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey("SelectedChoiceId")]
    [InverseProperty("AttemptAnswers")]
    public virtual QuestionChoice? SelectedChoice { get; set; }
}
