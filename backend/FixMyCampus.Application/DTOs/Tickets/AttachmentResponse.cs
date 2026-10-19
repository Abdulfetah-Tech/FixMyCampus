using FixMyCampus.Domain.Enums;

namespace FixMyCampus.Application.DTOs.Tickets;

public record AttachmentResponse(
    int Id,
    AttachmentType Type,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes,
    string UploadedByName,
    DateTime CreatedAt,
    string Url);