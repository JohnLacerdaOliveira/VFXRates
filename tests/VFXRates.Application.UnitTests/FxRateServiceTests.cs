using Microsoft.Extensions.Logging;
using Moq;
using VFXRates.Application.DTOs;
using VFXRates.Application.Interfaces;
using VFXRates.Application.Services;
using VFXRates.Domain.Entities;

namespace VFXRates.Application.UnitTests
{
    public class FxRateServiceTests
    {
        private readonly FxRateService _service;
        private readonly Mock<IFxRatesRepository> _mockRepo;
        private readonly Mock<IExchangeRateApiClient> _mockExchangeRateApiClient;
        private readonly Mock<IRabbitMqPublisher> _mockRabbitMQPublisher;
        private readonly Mock<ILogService> _mockLogger;

        public FxRateServiceTests()
        {
            _mockRepo = new Mock<IFxRatesRepository>();
            _mockExchangeRateApiClient = new Mock<IExchangeRateApiClient>();
            _mockRabbitMQPublisher = new Mock<IRabbitMqPublisher>();
            _mockLogger = new Mock<ILogService>();

            _service = new FxRateService(
                _mockRepo.Object,
                _mockExchangeRateApiClient.Object,
                _mockRabbitMQPublisher.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllFxRates_ShouldReturnRates_WhenDataExists()
        {
            // Arrange
            var fxRates = new List<FxRate>
            {
                new FxRate { BaseCurrency = "EUR", QuoteCurrency = "USD", Bid = 1.1m, Ask = 1.2m, LastUpdated = DateTime.Now },
                new FxRate { BaseCurrency = "GBP", QuoteCurrency = "USD", Bid = 1.3m, Ask = 1.4m, LastUpdated = DateTime.Now},
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(fxRates);

            // Act
            var result = await _service.GetAllFxRates();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllFxRates_ShouldReturnEmptyList_WhenNoDataExists()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<FxRate>());

            // Act
            var result = await _service.GetAllFxRates();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
            _mockRepo.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1000)]
        public async Task GetFxRateById_ShouldReturnNull_WhenRateNotFound(int id)
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((FxRate?)null);

            // Act
            var result = await _service.GetFxRateById(id);

            // Assert
            Assert.Null(result);
            _mockRepo.Verify(r => r.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task CreateFxRate_ShouldSaveAndReturnDto()
        {
            // Arrange
            var createDto = new CreateFxRateDto
            {
                BaseCurrency = "EUR",
                QuoteCurrency = "USD",
                Bid = 1.2345m,
                Ask = 1.3456m
            };

            FxRate? capturedEntity = null;
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<FxRate>()))
                     .Callback<FxRate>(fx => capturedEntity = fx);

            // Act
            var result = await _service.CreateFxRate(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EUR", result.BaseCurrency);
            Assert.Equal("USD", result.QuoteCurrency);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<FxRate>()), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
            Assert.NotNull(capturedEntity);
            Assert.Equal("EUR", capturedEntity!.BaseCurrency);
        }

        [Fact]
        public async Task CreateFxRate_ReturnsNull_WhenCurrencyPairAlreadyExists()
        {
            // Arrange
            var createDto = new CreateFxRateDto
            {
                BaseCurrency = "EUR",
                QuoteCurrency = "USD",
                Bid = 1.10m,
                Ask = 1.12m
            };

            var existingRate = new FxRate
            {
                BaseCurrency = createDto.BaseCurrency,
                QuoteCurrency = createDto.QuoteCurrency,
                Bid = 1.10m,
                Ask = 1.12m,
                LastUpdated = DateTime.UtcNow
            };

            // Mock the repository to return an existing rate
            _mockRepo.Setup(r => r.GetByCurrencyPairAsync(createDto.BaseCurrency, createDto.QuoteCurrency))
                     .ReturnsAsync(existingRate);

            // Act
            var result = await _service.CreateFxRate(createDto);

            // Assert
            Assert.Null(result); // Expecting the service to return null for a duplicate entry

            _mockRepo.Verify(r => r.GetByCurrencyPairAsync(createDto.BaseCurrency, createDto.QuoteCurrency), Times.Once);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<FxRate>()), Times.Never); // Ensure AddAsync was never called
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never); // Ensure SaveChangesAsync was never called
        }

        [Fact]
        public async Task UpdateFxRate_ShouldReturnNull_WhenEntityNotFound()
        {
            // Arrange
            var updateDto = new UpdateFxRateDto
            {
                BaseCurrency = "EUR",
                QuoteCurrency = "USD",
                Bid = 1.10m,
                Ask = 1.20m
            };
            _mockRepo.Setup(r => r.GetByCurrencyPairAsync("EUR", "USD"))
                     .ReturnsAsync((FxRate?)null);

            //Act
            var result = await _service.UpdateFxRate(updateDto);

            // Assert
            Assert.Null(result);
            _mockRepo.Verify(r => r.GetByCurrencyPairAsync("EUR", "USD"), Times.Once);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<FxRate>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task DeleteFxRate_ShouldReturnTrue_WhenFoundAndDeleted()
        {
            // Arrange
            var existingFxRate = new FxRate { 
                BaseCurrency = "GBP", 
                QuoteCurrency = "USD",
                Bid = 0.4343m, 
                Ask = 0.4567m,
                LastUpdated = DateTime.UtcNow
            };

            _mockRepo.Setup(r => r.GetByCurrencyPairAsync("GBP", "USD"))
                     .ReturnsAsync(existingFxRate);

            // Act
            var result = await _service.DeleteFxRate(existingFxRate.BaseCurrency, existingFxRate.QuoteCurrency);

            // Assert
            Assert.True(result);
            _mockRepo.Verify(r => r.GetByCurrencyPairAsync("GBP", "USD"), Times.Once);
            _mockRepo.Verify(r => r.DeleteAsync(existingFxRate), Times.Once);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteFxRate_ShouldReturnFalse_WhenNotFound()
        {
            //Arrange
            _mockRepo.Setup(r => r.GetByCurrencyPairAsync("GBP", "USD"))
                     .ReturnsAsync((FxRate?)null);

            // Act
            var result = await _service.DeleteFxRate("GBP", "USD");

            // Assert
            Assert.False(result);
            _mockRepo.Verify(r => r.GetByCurrencyPairAsync("GBP", "USD"), Times.Once);
            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<FxRate>()), Times.Never);
            _mockRepo.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public void NormalizeCurrencyPair_EnsuresUpperCaseAndTrimmed_WhendObjectIsICurrencyDto()
        {
            // Arrange
            var dto = new CreateFxRateDto { BaseCurrency = " usd ", QuoteCurrency = " eur " };

            // Act
            FxRateService.NormalizeCurrencyDto(dto);

            // Assert
            Assert.Equal("USD", dto.BaseCurrency);
            Assert.Equal("EUR", dto.QuoteCurrency);
        }

        [Fact]
        public void NormalizeCurrencyPair_EnsuresUpperCaseAndTrimmed_WhendObjectIsCurrencyPair()
        {
            // Arrange
            string BaseCurrency = "usd";
            string QuoteCurrency = "eur";

            // Act
            (BaseCurrency, QuoteCurrency) = FxRateService.NormalizeCurrencyPair(BaseCurrency, QuoteCurrency);

            // Assert
            Assert.Equal("USD", BaseCurrency);
            Assert.Equal("EUR", QuoteCurrency);
        }

        [Fact]
        public async Task GetFxRateById_MapsFxRateToDto_Correctly()
        {
            // Arrange
            var fxRate = new FxRate
            {
                BaseCurrency = "EUR",
                QuoteCurrency = "USD",
                Bid = 1.2345m,
                Ask = 1.3456m,
                LastUpdated = DateTime.UtcNow
            };

            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(fxRate);

            // Act
            var result = await _service.GetFxRateById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fxRate.BaseCurrency, result!.BaseCurrency);
            Assert.Equal(fxRate.QuoteCurrency, result.QuoteCurrency);
            Assert.Equal(fxRate.Bid, result.Bid);
            Assert.Equal(fxRate.Ask, result.Ask);
            Assert.Equal(fxRate.LastUpdated, result.LastUpdated);

            _mockRepo.Verify(r => r.GetByIdAsync(1), Times.Once);
        }
    }
}
