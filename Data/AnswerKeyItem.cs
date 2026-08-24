using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

[PrimaryKey("ExamId", "QuestionId")]
public partial class AnswerKeyItem
{
    [Key]
    public int ExamId { get; set; }

    [Key]
    public int QuestionId { get; set; }

    public int? CorrectChoiceId { get; set; }

    public bool? CorrectBool { get; set; }

    public string? ModelAnswer { get; set; }

    public int UploadedByTeacherId { get; set; }

    public DateTime UploadedAt { get; set; }

    [ForeignKey("CorrectChoiceId")]
    [InverseProperty("AnswerKeyItems")]
    public virtual QuestionChoice? CorrectChoice { get; set; }

    [ForeignKey("ExamId")]
    [InverseProperty("AnswerKeyItems")]
    public virtual Exam Exam { get; set; } = null!;

    [ForeignKey("QuestionId")]
    [InverseProperty("AnswerKeyItems")]
    public virtual Question Question { get; set; } = null!;

    [ForeignKey("UploadedByTeacherId")]
    [InverseProperty("AnswerKeyItems")]
    public virtual User UploadedByTeacher { get; set; } = null!;
}
