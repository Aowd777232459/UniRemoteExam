using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class TeacherProfile
{
    [Key]
    public int TeacherId { get; set; }

    [StringLength(150)]
    public string? Department { get; set; }

    [ForeignKey("TeacherId")]
    [InverseProperty("TeacherProfile")]
    public virtual User Teacher { get; set; } = null!;
}
