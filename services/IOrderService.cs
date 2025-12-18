using DemoApi.Models.Entities; // Pt LoginRequest, LoginResponse, ApiResponse
using DemoApi.Utils;
using DemoApi.Models;
using Stripe;

namespace DemoApi.Services;

public interface IOrderService
{
    public Task<ServiceResult<PaymentResponseDto>> CreatePaymentIntentAsync(int orderId, string userId, Role userRole);
}