using CollectorService.Models;

namespace CollectorService.Contracts
{
    public interface IElectricityPriceService
    {
        public Task<Result<object>> GetElectricityPricesAsync();
    }
}
