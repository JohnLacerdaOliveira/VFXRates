using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using VFXRates.Application.DTOs;
using VFXRates.Infrastructure.Data.dbContext;

namespace VFXRates.API.IntegrationTests
{
    public class FxRatesIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _webClient;
        private readonly WebApplicationFactory<Program> _factory;

        public FxRatesIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                // Set the environment to skip SQL Server registration and migrations.
                builder.UseEnvironment("IntegrationTest");

                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registration.
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<FxRatesDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Register an in-memory database.
                    services.AddDbContext<FxRatesDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("FxRatesTestDb");
                    });

                    // Ensure a fresh database state.
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<FxRatesDbContext>();
                    db.Database.EnsureDeleted();
                    db.Database.EnsureCreated();
                });
            });
            _webClient = _factory.CreateClient();
        }

        [Fact]
        public async Task GetFxRates_ReturnsRates_WhenDataExists()
        {
            // Arrange
            var testRates = new List<CreateFxRateDto>
            {
                new CreateFxRateDto { BaseCurrency = "USD", QuoteCurrency = "EUR", Bid = 1.10m, Ask = 1.12m },
                new CreateFxRateDto { BaseCurrency = "GBP", QuoteCurrency = "USD", Bid = 1.30m, Ask = 1.32m }
            };

            foreach (var rate in testRates)
            {
                var postResponse = await _webClient.PostAsJsonAsync("/api/FxRates", rate);
                postResponse.EnsureSuccessStatusCode();
            }

            // Act
            var response = await _webClient.GetAsync("/api/FxRates");

            // Assert
            response.EnsureSuccessStatusCode();
            var rates = await response.Content.ReadFromJsonAsync<List<FxRateDto>>();

            Assert.NotNull(rates);
            Assert.NotEmpty(rates);
            Assert.Equal(testRates.Count, rates.Count);

            // Verify the first entry matches the expected data
            Assert.Contains(rates, r => r.BaseCurrency == "USD" && r.QuoteCurrency == "EUR" && r.Bid == 1.10m && r.Ask == 1.12m);
            Assert.Contains(rates, r => r.BaseCurrency == "GBP" && r.QuoteCurrency == "USD" && r.Bid == 1.30m && r.Ask == 1.32m);
        }

        [Fact]
        public async Task GetFxRates_ReturnsEmptyList_WhenNoData()
        {
            var response = await _webClient.GetAsync("/api/FxRates");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var rates = await response.Content.ReadFromJsonAsync<List<FxRateDto>>();
            Assert.NotNull(rates);
            Assert.Empty(rates);
        }

        [Fact]
        public async Task GetFxRateByCurrencyPair_ReturnsCorrectRate()
        {
            // Arrange
            var newRate = new CreateFxRateDto
            {
                BaseCurrency = "GBP",
                QuoteCurrency = "USD",
                Bid = 1.30m,
                Ask = 1.32m
            };

            // Act
            var postResponse = await _webClient.PostAsJsonAsync("/api/FxRates", newRate);
            postResponse.EnsureSuccessStatusCode();

            var getResponse = await _webClient.GetAsync($"/api/FxRates/pair/{newRate.BaseCurrency}/{newRate.QuoteCurrency}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        }

        [Fact]
        public async Task PostFxRate_CreatesNewRate()
        {
            // Arrange
            var newRate = new CreateFxRateDto
            {
                BaseCurrency = "USD",
                QuoteCurrency = "EUR",
                Bid = 1.10m,
                Ask = 1.12m
            };

            // Act
            var response = await _webClient.PostAsJsonAsync("/api/FxRates", newRate);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task PostFxRate_ReturnsConflict_WhenDuplicateRateIsAdded()
        {
            // Assert
            var newRate = new CreateFxRateDto
            {
                BaseCurrency = "USD",
                QuoteCurrency = "EUR",
                Bid = 1.10m,
                Ask = 1.12m
            };

            // Act
            await _webClient.PostAsJsonAsync("/api/FxRates", newRate);
            var secondResponse = await _webClient.PostAsJsonAsync("/api/FxRates", newRate);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        }

        [Fact]
        public async Task PutFxRate_UpdatesExistingRate()
        {
            // Arrange: Create a new FX rate.
            var newRate = new CreateFxRateDto
            {
                BaseCurrency = "AUD",
                QuoteCurrency = "CAD",
                Bid = 0.90m,
                Ask = 0.92m
            };

            // Act: Post the new rate.
            var postResponse = await _webClient.PostAsJsonAsync("/api/FxRates", newRate);
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            // Optionally, wait a bit to ensure the record is persisted.
            await Task.Delay(200);

            // Arrange: Prepare the update DTO with matching route values.
            var updateDto = new UpdateFxRateDto
            {
                BaseCurrency = newRate.BaseCurrency,  // Must match the route.
                QuoteCurrency = newRate.QuoteCurrency,  // Must match the route.
                Bid = 0.95m,
                Ask = 0.97m
            };

            // Act: Send the PUT request.
            var putResponse = await _webClient.PutAsJsonAsync(
                $"/api/FxRates/{newRate.BaseCurrency}/{newRate.QuoteCurrency}", updateDto);

            // Assert: Verify that the response is OK.
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            // Optionally, verify that the updated values were applied.
            var updatedRate = await putResponse.Content.ReadFromJsonAsync<FxRateDto>();
            Assert.NotNull(updatedRate);
            Assert.Equal(updateDto.Bid, updatedRate.Bid);
            Assert.Equal(updateDto.Ask, updatedRate.Ask);
        }


        [Fact]
        public async Task PutFxRate_ReturnsNotFound_WhenRateDoesNotExist()
        {
            // Arrange
            var updateDto = new UpdateFxRateDto
            {
                BaseCurrency = "CHF",
                QuoteCurrency = "JPY",
                Bid = 120.50m,
                Ask = 121.00m
            };

            // Act
            var putResponse = await _webClient.PutAsJsonAsync(
                $"/api/FxRates/{updateDto.BaseCurrency}/{updateDto.QuoteCurrency}", updateDto);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteFxRate_RemovesExistingRate_ButRefetchedIfMissing()
        {
            // Arrange
            var newRate = new CreateFxRateDto
            {
                BaseCurrency = "JPY",
                QuoteCurrency = "USD",
                Bid = 110.50m,
                Ask = 110.75m
            };

            var postResponse = await _webClient.PostAsJsonAsync("/api/FxRates", newRate);
            postResponse.EnsureSuccessStatusCode();

            // Confirm the rate exists before attempting deletion.
            var getResponseBeforeDelete = await _webClient.GetAsync($"/api/FxRates/pair/{newRate.BaseCurrency}/{newRate.QuoteCurrency}");
            Assert.Equal(HttpStatusCode.OK, getResponseBeforeDelete.StatusCode);

            // Act
            var deleteResponse = await _webClient.DeleteAsync($"/api/FxRates/{newRate.BaseCurrency}/{newRate.QuoteCurrency}");
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Assert
            var getResponseAfterDelete = await _webClient.GetAsync($"/api/FxRates/pair/{newRate.BaseCurrency}/{newRate.QuoteCurrency}");
            Assert.Equal(HttpStatusCode.OK, getResponseAfterDelete.StatusCode);

            var refetchedRate = await getResponseAfterDelete.Content.ReadFromJsonAsync<FxRateDto>();
            Assert.NotNull(refetchedRate);
            Assert.Equal(newRate.BaseCurrency, refetchedRate.BaseCurrency);
            Assert.Equal(newRate.QuoteCurrency, refetchedRate.QuoteCurrency);
            Assert.NotEqual(newRate.Bid, refetchedRate.Bid); // Should be different from the deleted one
            Assert.NotEqual(newRate.Ask, refetchedRate.Ask); // Since it's fetched again
        }

        [Fact]
        public async Task DeleteFxRate_ReturnsNotFound_WhenRateDoesNotExist()
        {
            var deleteResponse = await _webClient.DeleteAsync("/api/FxRates/SGD/NZD");
            Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
        }
    }
}
