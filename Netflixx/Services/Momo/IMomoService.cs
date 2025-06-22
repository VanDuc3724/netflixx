using Netflixx.Models;
using Netflixx.Models.Momo;

namespace Netflixx.Services.Momo
{
    public interface IMomoService
    {
        Task<MomoCreatePaymentResponseModel> CreatePaymentMomo(OrderInfoModel request);
        MomoExecuteResponseModel MomoExecuteResponseModel(IQueryCollection collection);
    }
}
