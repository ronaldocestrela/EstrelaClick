namespace EstrelaClick.Backend.Application.DTOs;

public record EmailMessageDto(
    string To,
    string Subject,
    string? BodyHtml = null,
    string? BodyText = null,
    string? ToDisplayName = null,
    IReadOnlyList<string>? Cc = null,
    IReadOnlyList<string>? Bcc = null);
