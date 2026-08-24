using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class QuestionChoice
{
    [Key]
    public int ChoiceId { get; set; }

    public int QuestionId { get; set; }

    [StringLength(500)]
    public string ChoiceText { get; set; } = null!;

    public int SortOrder { get; set; }

    [InverseProperty("CorrectChoice")]
    public virtual ICollection<AnswerKeyItem> AnswerKeyItems { get; set; } = new List<AnswerKeyItem>();

    [InverseProperty("SelectedChoice")]
    public virtual ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();

    [ForeignKey("QuestionId")]
    [InverseProperty("QuestionChoices")]
    public virtual Question Question { get; set; } = null!;
}
