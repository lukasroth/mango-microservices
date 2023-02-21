using Mango.Web.Models.Dto;
using Mango.Web.Services.IServices;

namespace Mango.Web.Services.Services
{
    public interface IProductService : IBaseService
    {
        Task<T> GetAllProductAsync<T>(string token);
        Task<T> GetProductByIdAsync<T>(int id, string token);
        Task<T> CreateProductAsync<T>(ProductDto productDto, string token);
        Task<T>UpdateProductAsync<T>(ProductDto productDto, string token);
        Task<T> DeleteProductAsync<T>(int id, string token);
    }
}
