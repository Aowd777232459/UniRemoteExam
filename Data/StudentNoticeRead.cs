using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

[PrimaryKey("NoticeId", "StudentId")]
public partial class StudentNoticeRead
{
    [Key]
    public int NoticeId { get; set; }

    [Key]
    public int StudentId { get; set; }

    public DateTime ReadAt { get; set; }

    [ForeignKey("NoticeId")]
    [InverseProperty("StudentNoticeReads")]
    public virtual StudentNotice Notice { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("StudentNoticeReads")]
    public virtual User Student { get; set; } = null!;
}
