using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

[Index(nameof(Code), nameof(AcademicTermId), IsUnique = true)]
public class Course
{
    [Key]
    public int CourseId { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = null!;

    [Required, StringLength(200)]
    public string Name { get; set; } = null!;

    public int DepartmentId { get; set; }
    public int AcademicTermId { get; set; }
    public int TeacherId { get; set; }

    [StringLength(50)]
    public string? Level { get; set; }

    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(DepartmentId))]
    [InverseProperty(nameof(Department.Courses))]
    public Department Department { get; set; } = null!;

    [ForeignKey(nameof(AcademicTermId))]
    [InverseProperty(nameof(AcademicTerm.Courses))]
    public AcademicTerm AcademicTerm { get; set; } = null!;

    [ForeignKey(nameof(TeacherId))]
    [InverseProperty(nameof(User.CoursesTaught))]
    public User Teacher { get; set; } = null!;

    [InverseProperty(nameof(Exam.Course))]
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();

    [InverseProperty(nameof(CourseEnrollment.Course))]
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
}
