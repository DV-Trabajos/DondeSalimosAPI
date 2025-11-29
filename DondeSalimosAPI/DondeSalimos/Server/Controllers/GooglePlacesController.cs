using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace DondeSalimos.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GooglePlacesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<GooglePlacesController> _logger;

        public GooglePlacesController(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<GooglePlacesController> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        /// <summary>
        /// Buscar lugares cercanos usando Google Places API (Nearby Search)
        /// </summary>
        [HttpGet("nearby")]
        public async Task<IActionResult> NearbySearch(
            [FromQuery] double lat,
            [FromQuery] double lng,
            [FromQuery] string type,
            [FromQuery] string keyword = "",
            [FromQuery] int radius = 5000)
        {
            try
            {
                var apiKey = _configuration["GoogleMapsApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("Google Maps API Key no configurada");
                    return StatusCode(500, new { error = "Google Maps API Key no configurada" });
                }

                // URL de Google Places API
                var url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={lat},{lng}&radius={radius}&type={type}&key={apiKey}";

                if (!string.IsNullOrEmpty(keyword))
                {
                    url += $"&keyword={Uri.EscapeDataString(keyword)}";
                }

                _logger.LogInformation($"Buscando lugares en Google Places: {type} - Radio: {radius}m");

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error en Google Places API: {response.StatusCode}");
                    return StatusCode((int)response.StatusCode, content);
                }

                // Parsear respuesta para logging
                var jsonDoc = JsonDocument.Parse(content);
                var status = jsonDoc.RootElement.GetProperty("status").GetString();

                if (status == "OK")
                {
                    var resultsCount = jsonDoc.RootElement.GetProperty("results").GetArrayLength();
                    _logger.LogInformation($"Encontrados {resultsCount} lugares de tipo '{type}'");
                }
                else if (status == "ZERO_RESULTS")
                {
                    _logger.LogInformation($"No se encontraron lugares de tipo '{type}'");
                }
                else
                {
                    _logger.LogWarning($"Google Places API status: {status}");
                }

                // Retornar respuesta directamente
                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar lugares en Google Places");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Obtener detalles de un lugar específico
        /// </summary>
        [HttpGet("details/{placeId}")]
        public async Task<IActionResult> PlaceDetails(string placeId)
        {
            try
            {
                var apiKey = _configuration["GoogleMapsApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new { error = "Google Maps API Key no configurada" });
                }

                var url = $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&fields=name,formatted_address,formatted_phone_number,website,rating,opening_hours,photos,geometry&key={apiKey}";

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, content);
                }

                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener detalles del lugar");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Geocodificar una dirección
        /// </summary>
        [HttpGet("geocode")]
        public async Task<IActionResult> Geocode([FromQuery] string address)
        {
            try
            {
                var apiKey = _configuration["GoogleMapsApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    return StatusCode(500, new { error = "Google Maps API Key no configurada" });
                }

                var encodedAddress = Uri.EscapeDataString(address);
                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={apiKey}";

                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, content);
                }

                return Content(content, "application/json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al geocodificar dirección");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
