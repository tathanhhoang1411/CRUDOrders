using FluentValidation;
using CRUDOrders.Application.Commands;

namespace CRUDOrders.Application.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã sản phẩm không được để trống")
            .MaximumLength(50).WithMessage("Mã sản phẩm không được vượt quá 50 ký tự");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
            .MaximumLength(200).WithMessage("Tên sản phẩm không được vượt quá 200 ký tự");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Tồn kho không được âm");
    }
}

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("ID sản phẩm không hợp lệ");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
            .MaximumLength(200).WithMessage("Tên sản phẩm không được vượt quá 200 ký tự");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("Tồn kho không được âm");
    }
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("ID người dùng không hợp lệ");

        RuleFor(x => x.OrderItems)
            .NotEmpty().WithMessage("Đơn hàng phải có ít nhất một sản phẩm");

        RuleForEach(x => x.OrderItems).ChildRules(item =>
        {
            item.RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ID sản phẩm không hợp lệ");

            item.RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0");
        });
    }
}
