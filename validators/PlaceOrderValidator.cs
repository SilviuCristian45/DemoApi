using FluentValidation;
using DemoApi.Utils;

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

        RuleFor(x => x.PaymentType)
            .Must(method => method.Equals("CARD") || method.Equals("RAMBURS"))
            .WithMessage("Metoda de plată trebuie să fie 'Card' sau 'Ramburs'.");

        RuleFor(x => x.Address).NotNull().WithMessage("Adresa de livrare lipsește.");
        RuleFor(x => x.Address.City).NotEmpty().When(x => x.Address != null).WithMessage("Orașul este obligatoriu.");
        RuleFor(x => x.Address.Street).NotEmpty().When(x => x.Address != null).WithMessage("Strada este obligatorie.");
        RuleFor(x => x.Address.ZipCode).NotEmpty().When(x => x.Address != null).WithMessage("Codul poștal este obligatoriu.");

        RuleFor(x => x.PhoneNumber)
            .NotNull()
            .NotEmpty().WithMessage("Comanda trebuie sa contina un numar de telefon al destinatarului")
            .Matches(Constants.phoneNumberRegex)
            .WithMessage("Nr te telefon trebuie sa fie romanesc");
    }
}