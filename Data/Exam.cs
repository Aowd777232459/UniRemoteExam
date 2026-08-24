using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniRemoteExam.Data;

public partial class Exam
{
    [Key]
    public int ExamId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = null!;

    public int CourseId { get; set; }

    // Snapshot for legacy reports and safe upgrades. The relational source is CourseId.
    [StringLength(200)]
    public string? CourseName { get; set; }

    public int TeacherId { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Draft";

    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableTo { get; set; }
    public int MaxAttempts { get; set; } = 1;
    public int DurationMinutes { get; set; } = 60;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal PassPercentage { get; set; } = 50m;

    public bool AutoSubmitOnExpiry { get; set; } = true;
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleChoices { get; set; }
    public bool ShowCorrectAnswers { get; set; }

    [ForeignKey(nameof(CourseId))]
    [InverseProperty(nameof(Course.Exams))]
    public Course Course { get; set; } = null!;

    [ForeignKey(nameof(TeacherId))]
    [InverseProperty(nameof(User.Exams))]
    public User Teacher { get; set; } = null!;

    [InverseProperty(nameof(AnswerKeyItem.Exam))]
    public ICollection<AnswerKeyItem> AnswerKeyItems { get; set; } = new List<AnswerKeyItem>();

    [InverseProperty(nameof(ExamAttempt.Exam))]
    public ICollection<ExamAttempt> ExamAttempts { get; set; } = new List<ExamAttempt>();

    [InverseProperty(nameof(ExamPublishRequest.Exam))]
    public ICollection<ExamPublishRequest> ExamPublishRequests { get; set; } = new List<ExamPublishRequest>();

    [InverseProperty(nameof(Question.Exam))]
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
