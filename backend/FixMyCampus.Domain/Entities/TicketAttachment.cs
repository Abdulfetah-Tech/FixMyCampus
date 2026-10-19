using FixMyCampus.Domain.Common;
using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Domain.Entities;

public class TicketAttachment : BaseEntity
{
    public int TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public AttachmentType Type { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }

    public string UploadedByUserId { get; set; } = string.Empty;
    public AppUser UploadedByUser { get; set; } = null!;
}