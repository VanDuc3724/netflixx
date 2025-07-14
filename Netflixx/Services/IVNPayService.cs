using Microsoft.AspNetCore.Http;
namespace Netflixx.Services
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(decimal amount, string orderInfo, string returnUrl);
        bool ValidateResponse(IQueryCollection queryCollection);
    }
}
