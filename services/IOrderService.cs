using DemoApi.Models.Entities; // Pt LoginRequest, LoginResponse, ApiResponse
using DemoApi.Utils;
using DemoApi.Models;
using DemoApi.Data;
using Stripe;

namespace DemoApi.Services;

public interface IOrderService
{
    public Task<ServiceResult<PaymentResponseDto>> CreatePaymentIntentAsync(int orderId, string userId, Role userRole);
    public Task<ServiceResult<List<OrderResponse>>> GetAll(PaginatedQueryDto paginatedQueryDto);
    public Task<int> Total(PaginatedQueryDto paginatedQueryDto);

    public Task<ServiceResult<string>> Update(int orderId, UpdateOrderRequest updateOrderDto);
}