using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

[Index(nameof(CourseId), nameof(StudentId), IsUnique = true)]
public class CourseEnrollment
{
    [Key]
    public int EnrollmentId { get; set; }

    public int CourseId { get; set; }
    public int StudentId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(CourseId))]
    [InverseProperty(nameof(Course.Enrollments))]
    public Course Course { get; set; } = null!;

    [ForeignKey(nameof(StudentId))]
    [InverseProperty(nameof(User.CourseEnrollments))]
    public User Student { get; set; } = null!;
}
