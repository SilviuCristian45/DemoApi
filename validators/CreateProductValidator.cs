using FluentValidation;
using DemoApi.Models;

public class CreateProductValidator : AbstractValidator<CreateProductDto> {
    public CreateProductValidator() {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200).MinimumLength(2);
        RuleFor(x => x.Price).GreaterThan(0).LessThan(decimal.MaxValue).WithMessage("Prețul nu poate fi negativ!");
    }
}