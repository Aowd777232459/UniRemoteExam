using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

[Index(nameof(Code), IsUnique = true)]
public class Department
{
    [Key]
    public int DepartmentId { get; set; }

    [Required, StringLength(30)]
    public string Code { get; set; } = null!;

    [Required, StringLength(150)]
    public string Name { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    [InverseProperty(nameof(Course.Department))]
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
