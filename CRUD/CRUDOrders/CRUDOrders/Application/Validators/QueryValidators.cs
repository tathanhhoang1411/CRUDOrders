using FluentValidation;
using Microsoft.Extensions.Options;
using CRUDOrders.Application.Common;
using CRUDOrders.Application.Queries;

namespace CRUDOrders.Application.Validators;

public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator(IOptions<PaginationSettings> options)
    {
        var settings = options.Value;

        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(settings.MinSkip)
            .WithMessage($"Tham so 'skip' khong duoc nho hon {settings.MinSkip}")
            .LessThanOrEqualTo(settings.MaxSkip)
            .WithMessage($"Tham so 'skip' phai be hon 100 (toi da la {settings.MaxSkip})");

        RuleFor(x => x.Take)
            .GreaterThanOrEqualTo(settings.MinTake)
            .WithMessage($"Tham so 'take' khong duoc nho hon {settings.MinTake}")
            .LessThanOrEqualTo(settings.MaxTake)
            .WithMessage($"Tham so 'take' khong duoc vuot qua {settings.MaxTake}");
    }
}

public class GetOrdersQueryValidator : AbstractValidator<GetOrdersQuery>
{
    public GetOrdersQueryValidator(IOptions<PaginationSettings> options)
    {
        var settings = options.Value;

        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(settings.MinSkip)
            .WithMessage($"Tham so 'skip' khong duoc nho hon {settings.MinSkip}")
            .LessThanOrEqualTo(settings.MaxSkip)
            .WithMessage($"Tham so 'skip' phai be hon 100 (toi da la {settings.MaxSkip})");

        RuleFor(x => x.Take)
            .GreaterThanOrEqualTo(settings.MinTake)
            .WithMessage($"Tham so 'take' khong duoc nho hon {settings.MinTake}")
            .LessThanOrEqualTo(settings.MaxTake)
            .WithMessage($"Tham so 'take' khong duoc vuot qua {settings.MaxTake}");
    }
}

public class GetUserOrdersQueryValidator : AbstractValidator<GetUserOrdersQuery>
{
    public GetUserOrdersQueryValidator(IOptions<PaginationSettings> options)
    {
        var settings = options.Value;

        RuleFor(x => x.Skip)
            .GreaterThanOrEqualTo(settings.MinSkip)
            .WithMessage($"Tham so 'skip' khong duoc nho hon {settings.MinSkip}")
            .LessThanOrEqualTo(settings.MaxSkip)
            .WithMessage($"Tham so 'skip' phai be hon 100 (toi da la {settings.MaxSkip})");

        RuleFor(x => x.Take)
            .GreaterThanOrEqualTo(settings.MinTake)
            .WithMessage($"Tham so 'take' khong duoc nho hon {settings.MinTake}")
            .LessThanOrEqualTo(settings.MaxTake)
            .WithMessage($"Tham so 'take' khong duoc vuot qua {settings.MaxTake}");
    }
}
