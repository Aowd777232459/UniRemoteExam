using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

[Index(nameof(Name), IsUnique = true)]
public class AcademicTerm
{
    [Key]
    public int AcademicTermId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    [InverseProperty(nameof(Course.AcademicTerm))]
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
