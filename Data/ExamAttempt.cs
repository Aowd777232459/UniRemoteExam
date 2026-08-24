using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniRemoteExam.Data;

public partial class ExamAttempt
{
    [Key]
    public int AttemptId { get; set; }
    public int ExamId { get; set; }
    public int StudentId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Started";

    public bool AutoSubmitted { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal? AutoScore { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal? ManualScore { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal? FinalScore { get; set; }

    [Column(TypeName = "decimal(8, 2)")]
    public decimal? MaximumScore { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? Percentage { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? PassPercentage { get; set; }

    public bool? IsPassed { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public int? FinalizedByUserId { get; set; }

    [ForeignKey(nameof(ExamId))]
    [InverseProperty(nameof(Exam.ExamAttempts))]
    public Exam Exam { get; set; } = null!;

    [ForeignKey(nameof(StudentId))]
    [InverseProperty(nameof(User.ExamAttempts))]
    public User Student { get; set; } = null!;

    [ForeignKey(nameof(FinalizedByUserId))]
    [InverseProperty(nameof(User.FinalizedAttempts))]
    public User? FinalizedByUser { get; set; }

    [InverseProperty(nameof(AttemptAnswer.Attempt))]
    public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = new List<AttemptAnswer>();

    [InverseProperty(nameof(ProctorEvent.Attempt))]
    public ICollection<ProctorEvent> ProctorEvents { get; set; } = new List<ProctorEvent>();

    [InverseProperty(nameof(global::UniRemoteExam.Data.ManualScore.Attempt))]
    public ICollection<ManualScore> ManualScores { get; set; } = new List<ManualScore>();
}
