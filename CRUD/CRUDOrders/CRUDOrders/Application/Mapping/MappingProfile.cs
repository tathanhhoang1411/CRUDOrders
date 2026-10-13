using AutoMapper;
using CRUDOrders.Application.DTOs;
using CRUDOrders.Core.Entities;

namespace CRUDOrders.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Product, ProductDto>().ReverseMap();
        CreateMap<Order, OrderDto>().ReverseMap();
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductName, opt => opt.Ignore())
            .ReverseMap();
    }
}
