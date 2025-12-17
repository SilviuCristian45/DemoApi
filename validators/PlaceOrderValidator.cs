using FluentValidation;
using DemoApi.Models;

public class PlaceOrderValidator: AbstractValidator<PlaceOrderRequest> {
    public PlaceOrderValidator() {
       RuleFor(x => x.Items)
            .NotNull()
            .NotEmpty().WithMessage("Comanda trebuie să conțină cel puțin un produs.");
        RuleForEach(x => x.Items).ChildRules(items => 
        {
            items.RuleFor(x => x.ProductId).GreaterThan(0);
            items.RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Cantitatea trebuie să fie minim 1.");
        });
    }
}