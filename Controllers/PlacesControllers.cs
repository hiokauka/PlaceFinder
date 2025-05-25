using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

namespace ItineraryPlanner.Controllers
{
    [Route("Places")]
    public class PlacesController : Controller
    {
        private readonly string _apiKey;

        public PlacesController(IConfiguration configuration)
        {
            _apiKey = configuration["GoogleAPI:Key"];
        }
        [HttpGet("Search")]
        public async Task<IActionResult> Search(string query)
        {
            using var client = new HttpClient();
            var url = $"https://maps.googleapis.com/maps/api/place/textsearch/json?query={Uri.EscapeDataString(query)}&key={_apiKey}";

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("results", out var results))
            {
                return Json(new { places = new object[0] });
            }

            var placesList = new List<object>();

            foreach (var place in results.EnumerateArray())
            {
                var name = place.GetProperty("name").GetString();

                // Check if photos exist and how many
                if (place.TryGetProperty("photos", out var photos))
                {
                    var photoCount = photos.GetArrayLength();
                    Console.WriteLine($"Place '{name}' has {photoCount} photos.");
                }
                else
                {
                    Console.WriteLine($"Place '{name}' has NO photos.");
                }

                var placeObj = new
                {
                    name = name,
                    address = place.TryGetProperty("formatted_address", out var addr) ? addr.GetString() : "N/A",
                    rating = place.TryGetProperty("rating", out var rating) ? rating.GetDecimal() : (decimal?)null,
                    types = place.TryGetProperty("types", out var types) ? types.EnumerateArray().Select(t => t.GetString()).ToArray() : new string[0],
                    photo = place.TryGetProperty("photos", out var photos2) && photos2.GetArrayLength() > 0
                        ? $"https://maps.googleapis.com/maps/api/place/photo?maxwidth=400&photoreference={photos2[0].GetProperty("photo_reference").GetString()}&key={_apiKey}"
                        : null
                };

                placesList.Add(placeObj);
            }

            return Json(new { places = placesList });
        }

    }
}
