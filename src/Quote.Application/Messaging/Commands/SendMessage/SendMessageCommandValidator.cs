using FluentValidation;

namespace Quote.Application.Messaging.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId)
            .NotEmpty().WithMessage("Conversation ID is required");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Message content is required")
            .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters");

        RuleFor(x => x.MediaUrl)
            .MaximumLength(500).When(x => x.MediaUrl != null)
            .WithMessage("Media URL cannot exceed 500 characters");

        RuleFor(x => x.MediaType)
            .Must(x => x == null || x == "image" || x == "file")
            .WithMessage("Media type must be 'image' or 'file'");

        RuleFor(x => x.FileName)
            .MaximumLength(255).When(x => x.FileName != null)
            .WithMessage("File name cannot exceed 255 characters");
    }
}
