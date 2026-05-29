using eShop.WebApp.Services;
using FluentValidation;

namespace eShop.WebApp.Validators;

public class CheckoutValidator : AbstractValidator<BasketCheckoutInfo>
{
    public CheckoutValidator()
    {
        RuleFor(x => x.Street).NotEmpty().WithMessage("Address is required.");
        RuleFor(x => x.City).NotEmpty().WithMessage("City is required.");
        RuleFor(x => x.State).NotEmpty().WithMessage("State is required.");
        RuleFor(x => x.Country).NotEmpty().WithMessage("Country is required.");
        RuleFor(x => x.ZipCode).NotEmpty().WithMessage("Zip code is required.");
    }
}
