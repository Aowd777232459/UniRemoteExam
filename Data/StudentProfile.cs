using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

[Index("StudentNumber", Name = "UQ__StudentP__DD81BF6C5876DA74", IsUnique = true)]
public partial class StudentProfile
{
    [Key]
    public int StudentId { get; set; }

    [StringLength(50)]
    public string? StudentNumber { get; set; }

    [StringLength(50)]
    public string? Level { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("StudentProfile")]
    public virtual User Student { get; set; } = null!;
}
