using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class Question
{
    [Key]
    public int QuestionId { get; set; }

    public int ExamId { get; set; }

    [StringLength(20)]
    public string QuestionType { get; set; } = null!;

    public string Body { get; set; } = null!;

    [Column(TypeName = "decimal(6, 2)")]
    public decimal Points { get; set; }

    public int? TimeLimitSeconds { get; set; }

    public int SortOrder { get; set; }

    [InverseProperty("Question")]
    public virtual ICollection<AnswerKeyItem> AnswerKeyItems { get; set; } = new List<AnswerKeyItem>();

    [InverseProperty("Question")]
    public virtual ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();

    [ForeignKey("ExamId")]
    [InverseProperty("Questions")]
    public virtual Exam Exam { get; set; } = null!;

    [InverseProperty("Question")]
    public virtual ICollection<ManualScore> ManualScores { get; set; } = new List<ManualScore>();

    [InverseProperty("Question")]
    public virtual ICollection<QuestionChoice> QuestionChoices { get; set; } = new List<QuestionChoice>();

    [InverseProperty("Question")]
    public virtual ICollection<ProctorEvent> ProctorEvents { get; set; } = new List<ProctorEvent>();
}
