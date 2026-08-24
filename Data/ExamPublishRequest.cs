using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class ExamPublishRequest
{
    [Key]
    public int RequestId { get; set; }

    public int ExamId { get; set; }

    public int TeacherId { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public int? ReviewedByAdminId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    [StringLength(500)]
    public string? AdminNote { get; set; }

    [ForeignKey("ExamId")]
    [InverseProperty("ExamPublishRequests")]
    public virtual Exam Exam { get; set; } = null!;

    [ForeignKey("ReviewedByAdminId")]
    [InverseProperty("ExamPublishRequestReviewedByAdmins")]
    public virtual User? ReviewedByAdmin { get; set; }

    [ForeignKey("TeacherId")]
    [InverseProperty("ExamPublishRequestTeachers")]
    public virtual User Teacher { get; set; } = null!;
}
