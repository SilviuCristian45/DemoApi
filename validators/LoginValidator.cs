using FluentValidation;
using DemoApi.Models;

public class CreateLoginValidator : AbstractValidator<LoginRequest> {
    public CreateLoginValidator() {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(10).MinimumLength(2);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(20).MinimumLength(2);
    }
}