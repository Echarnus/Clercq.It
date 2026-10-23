using FluentValidation;
using MediatR;

namespace Clercq.It.Application.Features.MyZen;

public record StartMyZenLoginCommand : IRequest<MyZenLoginStepDto>;

public record SubmitMyZenLoginCommand(string? Captcha) : IRequest<MyZenLoginStepDto>;

public record SubmitMyZenMfaCodeCommand(string Code) : IRequest<MyZenLoginStepDto>;

public record DisconnectMyZenCommand : IRequest;

public class SubmitMyZenLoginCommandValidator : AbstractValidator<SubmitMyZenLoginCommand>
{
    public SubmitMyZenLoginCommandValidator()
    {
        RuleFor(x => x.Captcha)
            .MaximumLength(20).WithMessage("Captcha must not exceed 20 characters");
    }
}

public class SubmitMyZenMfaCodeCommandValidator : AbstractValidator<SubmitMyZenMfaCodeCommand>
{
    public SubmitMyZenMfaCodeCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required")
            .Matches("^[0-9]{6}$").WithMessage("Code must be 6 digits");
    }
}

public class StartMyZenLoginCommandHandler(IMyZenGateway gateway) : IRequestHandler<StartMyZenLoginCommand, MyZenLoginStepDto>
{
    public Task<MyZenLoginStepDto> Handle(StartMyZenLoginCommand request, CancellationToken cancellationToken) =>
        gateway.StartLoginAsync(cancellationToken);
}

public class SubmitMyZenLoginCommandHandler(IMyZenGateway gateway) : IRequestHandler<SubmitMyZenLoginCommand, MyZenLoginStepDto>
{
    public Task<MyZenLoginStepDto> Handle(SubmitMyZenLoginCommand request, CancellationToken cancellationToken) =>
        gateway.SubmitLoginAsync(string.IsNullOrWhiteSpace(request.Captcha) ? null : request.Captcha.Trim(), cancellationToken);
}

public class SubmitMyZenMfaCodeCommandHandler(IMyZenGateway gateway) : IRequestHandler<SubmitMyZenMfaCodeCommand, MyZenLoginStepDto>
{
    public Task<MyZenLoginStepDto> Handle(SubmitMyZenMfaCodeCommand request, CancellationToken cancellationToken) =>
        gateway.SubmitMfaCodeAsync(request.Code, cancellationToken);
}

public class DisconnectMyZenCommandHandler(IMyZenGateway gateway) : IRequestHandler<DisconnectMyZenCommand>
{
    public Task Handle(DisconnectMyZenCommand request, CancellationToken cancellationToken) =>
        gateway.DisconnectAsync(cancellationToken);
}
