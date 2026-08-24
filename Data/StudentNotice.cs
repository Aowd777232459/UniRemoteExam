using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace UniRemoteExam.Data;

public partial class StudentNotice
{
    [Key]
    public int NoticeId { get; set; }

    [StringLength(200)]
    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("Notice")]
    public virtual ICollection<StudentNoticeRead> StudentNoticeReads { get; set; } = new List<StudentNoticeRead>();
}
