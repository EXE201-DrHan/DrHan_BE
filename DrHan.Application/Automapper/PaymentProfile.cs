using AutoMapper;
using DrHan.Application.DTOs.Payment;
using DrHan.Domain.Entities.Users;

namespace DrHan.Application.Automapper
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile()
        {
            CreateMap<Payment, PaymentResponseDto>()
                .ForMember(dest => dest.PaymentUrl, opt => opt.Ignore())
                .ReverseMap();

            CreateMap<CreatePaymentRequestDto, Payment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.BusinessId, opt => opt.Ignore())
                .ForMember(dest => dest.TransactionId, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentStatus, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentDate, opt => opt.Ignore())
                .ForMember(dest => dest.FailureReason, opt => opt.Ignore())
                .ForMember(dest => dest.UserSubscription, opt => opt.Ignore())
                .ForMember(dest => dest.CreateAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdateAt, opt => opt.Ignore());
        }
    }
}