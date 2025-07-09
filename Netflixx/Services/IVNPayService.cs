namespace Netflixx.Services
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(decimal amount, string orderInfo, string returnUrl);
    }
}
