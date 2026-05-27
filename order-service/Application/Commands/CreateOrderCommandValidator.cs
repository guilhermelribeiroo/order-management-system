using FluentValidation;

namespace Application.Commands
{
    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required");

            RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must contain at least one item");

            RuleForEach(x => x.Items)
            .ChildRules(items =>
            {
                items.RuleFor(i => i.ProductId)
                    .NotEmpty()
                    .WithMessage("ProductId is required");

                items.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithMessage("Quantity must be greater than zero");

                items.RuleFor(i => i.UnitPrice)
                    .GreaterThan(0)
                    .WithMessage("Unit price must be greater than zero");
            });
        }
    }
}
