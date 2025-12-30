using AutoMapper;
using DemoApi.Data; // Namespace-ul unde sunt Order si OrderResponse
using DemoApi.Utils;
using DemoApi.Models.Entities;
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Îi spui: "Când îți cer, copiază datele din Order în OrderResponse"
        CreateMap<Order, OrderResponse>()
            .ForMember(dest => dest.PaymentIntentId, opt => opt.MapFrom(src => src.PaymentIntentId ?? string.Empty));
        CreateMap<OrderItem, OrderItemResponse>()
            // Configurare specială: Numele produsului se află în tabela "Product"
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name ?? ""));
    }
}