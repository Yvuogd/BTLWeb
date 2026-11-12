using System.ComponentModel.DataAnnotations;

namespace NovelPlatform.Models;

public sealed class NovelInput
{
    [Required(ErrorMessage = "Nhập tên truyện."), StringLength(180)]
    public string Title { get; set; } = "";

    [StringLength(2500)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Chọn thể loại.")]
    public int CategoryId { get; set; }

    [Required, RegularExpression("Ongoing|Completed")]
    public string Status { get; set; } = "Ongoing";

    public IFormFile? Cover { get; set; }
}

public sealed class ChapterInput
{
    [Range(1, int.MaxValue, ErrorMessage = "Số chương phải lớn hơn 0.")]
    public int Number { get; set; }

    [Required(ErrorMessage = "Nhập tên chương."), StringLength(180)]
    public string Title { get; set; } = "";

    [Required(ErrorMessage = "Nhập nội dung chương."), StringLength(100000)]
    public string Content { get; set; } = "";
}
