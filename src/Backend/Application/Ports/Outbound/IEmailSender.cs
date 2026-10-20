using EstrelaClick.Backend.Application.DTOs;
using EstrelaClick.Shared.SharedKernel.Results;

namespace EstrelaClick.Backend.Application.Ports.Outbound;

public interface IEmailSender
{
    Task<Result> SendEmailAsync(EmailMessageDto message, CancellationToken cancellationToken = default);
}
