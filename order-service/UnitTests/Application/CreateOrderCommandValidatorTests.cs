using Application.Commands;
using FluentValidation.TestHelper;

namespace UnitTests.Application
{
    public class CreateOrderCommandValidatorTests
    {
        private readonly CreateOrderCommandValidator _validator = new();

        [Fact]
        public void Should_HaveError_When_CustomerIdIsEmpty()
        {
            var command = BuildValidCommand();
            command.CustomerId = Guid.Empty;

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.CustomerId)
                  .WithErrorMessage("CustomerId is required");
        }

        [Fact]
        public void Should_NotHaveError_When_CustomerIdIsValid()
        {
            var command = BuildValidCommand();

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.CustomerId);
        }

        [Fact]
        public void Should_HaveError_When_ItemsIsEmpty()
        {
            var command = BuildValidCommand();
            command.Items = new List<CreateOrderCommand.OrderItemDto>();

            var result = _validator.TestValidate(command);

            result.ShouldHaveValidationErrorFor(x => x.Items)
                  .WithErrorMessage("Order must contain at least one item");
        }

        [Fact]
        public void Should_NotHaveError_When_ItemsHasAtLeastOneItem()
        {
            var command = BuildValidCommand();

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveValidationErrorFor(x => x.Items);
        }

        [Fact]
        public void Should_HaveError_When_ItemProductIdIsEmpty()
        {
            var command = BuildValidCommand();
            command.Items[0].ProductId = Guid.Empty;

            var result = _validator.TestValidate(command);

            Assert.Contains(result.Errors, e => e.ErrorMessage == "ProductId is required");
        }

        [Fact]
        public void Should_HaveError_When_ItemQuantityIsZero()
        {
            var command = BuildValidCommand();
            command.Items[0].Quantity = 0;

            var result = _validator.TestValidate(command);

            Assert.Contains(result.Errors, e => e.ErrorMessage == "Quantity must be greater than zero");
        }

        [Fact]
        public void Should_HaveError_When_ItemQuantityIsNegative()
        {
            var command = BuildValidCommand();
            command.Items[0].Quantity = -1;

            var result = _validator.TestValidate(command);

            Assert.Contains(result.Errors, e => e.ErrorMessage == "Quantity must be greater than zero");
        }

        [Fact]
        public void Should_HaveError_When_ItemUnitPriceIsZero()
        {
            var command = BuildValidCommand();
            command.Items[0].UnitPrice = 0;

            var result = _validator.TestValidate(command);

            Assert.Contains(result.Errors, e => e.ErrorMessage == "Unit price must be greater than zero");
        }

        [Fact]
        public void Should_HaveError_When_ItemUnitPriceIsNegative()
        {
            var command = BuildValidCommand();
            command.Items[0].UnitPrice = -5m;

            var result = _validator.TestValidate(command);

            Assert.Contains(result.Errors, e => e.ErrorMessage == "Unit price must be greater than zero");
        }

        [Fact]
        public void Should_NotHaveAnyErrors_When_CommandIsValid()
        {
            var command = BuildValidCommand();

            var result = _validator.TestValidate(command);

            result.ShouldNotHaveAnyValidationErrors();
        }

        // --- Helpers ---
        private static CreateOrderCommand BuildValidCommand() => new()
        {
            CustomerId = Guid.NewGuid(),
            Items = new List<CreateOrderCommand.OrderItemDto>
            {
                new()
                {
                    ProductId = Guid.NewGuid(),
                    ProductName = "Test Product",
                    UnitPrice = 10.00m,
                    Quantity = 1
                }
            }
        };
    }
}