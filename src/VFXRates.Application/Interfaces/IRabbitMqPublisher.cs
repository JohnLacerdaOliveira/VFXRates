using VFXRates.Domain.Entities;

namespace VFXRates.Application.Interfaces
{
    public interface IRabbitMqPublisher
    {
        Task PublishFxRateCreation(FxRate newRate);
    }
}
