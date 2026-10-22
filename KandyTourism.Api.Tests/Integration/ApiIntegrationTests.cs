using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using KandyTourism.Api.Tests.Infrastructure;

namespace KandyTourism.Api.Tests.Integration;

[Collection(ApiCollection.Name)]
public sealed class ApiIntegrationTests(ApiFactory factory)
{
    private const string TokenHeader = "X-Itinerary-Token";
    private readonly HttpClient _client = factory.CreateClient();

    private static async Task<JsonNode> Json(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync())!;

    private async Task<int> AttractionIdBySlug(string slug) =>
        (int)(await Json(await _client.GetAsync($"/api/v1/attractions/by-slug/{slug}")))["id"]!;

    [Fact]
    public async Task Attraction_list_returns_seeded_data_in_the_frontend_shape()
    {
        var response = await _client.GetAsync("/api/v1/attractions?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await Json(response);
        Assert.True((int)body["totalCount"]! >= 15);
        var first = body["items"]!.AsArray()[0]!.AsObject();
        Assert.Equal("temple-of-the-sacred-tooth-relic", (string)first["slug"]!);
        foreach (var field in new[] { "id", "name", "category", "categoryName", "shortDescription", "imageUrl", "imageAlt",
                     "area", "latitude", "longitude", "coordinatesApproximate", "estimatedVisitDurationMinutes", "featured" })
        {
            Assert.True(first.ContainsKey(field), $"Missing '{field}'");
        }
        Assert.False(first.ContainsKey("searchText"));
        Assert.False(first.ContainsKey("isActive"));
    }

    [Fact]
    public async Task Search_and_category_filter_run_on_the_server()
    {
        var body = await Json(await _client.GetAsync("/api/v1/attractions?q=temple&category=religious&sort=name"));

        var names = body["items"]!.AsArray().Select(i => (string)i!["name"]!).ToList();
        Assert.Equal(["Degaldoruwa Raja Maha Vihara", "Gadaladeniya Vihara", "Lankatilaka Vihara", "Temple of the Sacred Tooth Relic"], names);
    }

    [Fact]
    public async Task Invalid_query_returns_validation_problem_details_with_camel_case_keys()
    {
        var response = await _client.GetAsync("/api/v1/attractions?sort=evil&pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = (await Json(response))["errors"]!.AsObject();
        Assert.True(errors.ContainsKey("sort"));
        Assert.True(errors.ContainsKey("pageSize"));
    }

    [Fact]
    public async Task Unknown_slug_returns_404_problem_with_code()
    {
        var response = await _client.GetAsync("/api/v1/attractions/by-slug/no-such-place");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("attraction-not-found", (string)(await Json(response))["code"]!);
    }

    [Fact]
    public async Task Guest_itinerary_persists_stops_order_and_start()
    {
        var create = await _client.PostAsync("/api/v1/itineraries", null);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.NotNull(create.Headers.Location);
        var created = await Json(create);
        var id = (string)created["itinerary"]!["id"]!;
        var token = (string)created["accessToken"]!;

        int lake = await AttractionIdBySlug("kandy-lake"), temple = await AttractionIdBySlug("temple-of-the-sacred-tooth-relic"),
            buddha = await AttractionIdBySlug("bahirawakanda-buddha-statue"), gardens = await AttractionIdBySlug("royal-botanical-gardens-peradeniya");

        foreach (var attractionId in new[] { lake, temple, buddha, gardens })
        {
            var add = await Send(HttpMethod.Post, $"/api/v1/itineraries/{id}/stops", token, new { attractionId });
            Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        }

        var reorder = await Send(HttpMethod.Put, $"/api/v1/itineraries/{id}/stops/order", token,
            new { attractionIds = new[] { gardens, lake, temple, buddha } });
        Assert.Equal(HttpStatusCode.OK, reorder.StatusCode);

        var start = await Send(HttpMethod.Put, $"/api/v1/itineraries/{id}/start", token,
            new { type = "preset", presetSlug = "kandy-railway-station" });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);

        var reloaded = await Json(await Send(HttpMethod.Get, $"/api/v1/itineraries/{id}", token));
        var stops = reloaded["stops"]!.AsArray();
        Assert.Equal([gardens, lake, temple, buddha], stops.Select(s => (int)s!["attraction"]!["id"]!));
        Assert.Equal([1, 2, 3, 4], stops.Select(s => (int)s!["order"]!));
        Assert.Equal("preset", (string)reloaded["start"]!["type"]!);
        Assert.Equal("Kandy Railway Station", (string)reloaded["start"]!["label"]!);
        Assert.NotNull(stops[0]!["attraction"]!["latitude"]);

        var duplicate = await Send(HttpMethod.Post, $"/api/v1/itineraries/{id}/stops", token, new { attractionId = lake });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("duplicate-stop", (string)(await Json(duplicate))["code"]!);

        var removed = await Json(await Send(HttpMethod.Delete, $"/api/v1/itineraries/{id}/stops/{lake}", token));
        Assert.Equal([gardens, temple, buddha], removed["stops"]!.AsArray().Select(s => (int)s!["attraction"]!["id"]!));
    }

    [Fact]
    public async Task Guest_itinerary_cannot_be_read_without_its_token()
    {
        var created = await Json(await _client.PostAsync("/api/v1/itineraries", null));
        var id = (string)created["itinerary"]!["id"]!;
        var other = await Json(await _client.PostAsync("/api/v1/itineraries", null));

        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync($"/api/v1/itineraries/{id}")).StatusCode);
        var wrong = await Send(HttpMethod.Get, $"/api/v1/itineraries/{id}", (string)other["accessToken"]!);
        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
    }

    [Fact]
    public async Task Admin_endpoints_reject_anonymous_and_invalid_tokens()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/v1/admin/dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.DeleteAsync("/api/v1/admin/attractions/1")).StatusCode);

        using var forged = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/dashboard");
        forged.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJyb2xlIjoiQWRtaW4ifQ.invalid");
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.SendAsync(forged)).StatusCode);
    }

    [Fact]
    public async Task Admin_login_failure_is_generic()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = ApiFactory.AdminEmail, password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("invalid-credentials", (string)(await Json(response))["code"]!);
    }

    [Fact]
    public async Task Signed_in_admin_can_create_update_and_delete_attractions()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { email = ApiFactory.AdminEmail, password = ApiFactory.AdminPassword });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (string)(await Json(login))["accessToken"]!;
        var admin = new AuthenticationHeaderValue("Bearer", token);

        var draft = new
        {
            name = "Integration Test Garden",
            slug = "integration-test-garden",
            category = "nature",
            shortDescription = "Created by an integration test.",
            description = new[] { "A paragraph about the garden." },
            imageUrl = "/images/attraction-placeholder.svg",
            imageAlt = "Placeholder image",
            gallery = Array.Empty<object>(),
            address = "Test Road, Kandy",
            area = "Kandy City",
            latitude = 7.29,
            longitude = 80.63,
            coordinatesApproximate = true,
            estimatedVisitDurationMinutes = 30,
            visitorTips = new[] { "Bring water." },
            featured = false,
            tags = new[] { "integration" },
        };

        var create = await SendAuthorized(HttpMethod.Post, "/api/v1/admin/attractions", admin, draft);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var id = (int)(await Json(create))["id"]!;

        var invalid = await SendAuthorized(HttpMethod.Put, $"/api/v1/admin/attractions/{id}", admin, draft with { latitude = 51.5 });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.True((await Json(invalid))["errors"]!.AsObject().ContainsKey("latitude"));

        var update = await SendAuthorized(HttpMethod.Put, $"/api/v1/admin/attractions/{id}", admin, draft with { featured = true });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.True((bool)(await Json(update))["featured"]!);

        var dashboard = await SendAuthorized(HttpMethod.Get, "/api/v1/admin/dashboard", admin);
        Assert.Equal(HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Equal(6, (int)(await Json(dashboard))["byCategory"]!.AsArray().Count);

        Assert.Equal(HttpStatusCode.NoContent, (await SendAuthorized(HttpMethod.Delete, $"/api/v1/admin/attractions/{id}", admin)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/v1/attractions/{id}")).StatusCode);
    }

    [Fact]
    public async Task Cors_allows_only_the_configured_frontend_origin()
    {
        async Task<HttpResponseMessage> Preflight(string origin)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/itineraries");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "content-type,x-itinerary-token");
            return await _client.SendAsync(request);
        }

        var allowed = await Preflight("http://localhost:4200");
        Assert.Equal("http://localhost:4200", allowed.Headers.GetValues("Access-Control-Allow-Origin").Single());

        var blocked = await Preflight("https://evil.example");
        Assert.False(blocked.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private Task<HttpResponseMessage> Send(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add(TokenHeader, token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> SendAuthorized(HttpMethod method, string url, AuthenticationHeaderValue auth, object? body = null)
    {
        var request = new HttpRequestMessage(method, url) { Headers = { Authorization = auth } };
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        return _client.SendAsync(request);
    }
}
