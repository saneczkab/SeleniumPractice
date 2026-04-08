using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SeleniumPractice;

public class ApiClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _username;
    private readonly string _password;
    

    public ApiClient(string baseUrl, string username, string password)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _username = username;
        _password = password;
    }

    public void Dispose() => _httpClient.Dispose();
    
    public async Task LoginAsync()
    {
        var loginData = new { Email = _username, Password = _password };
        var response = await _httpClient.PostAsJsonAsync("/api/v1/auth", loginData);
        response.EnsureSuccessStatusCode();
        
        var token = await response.Content.ReadAsStringAsync();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string> CreateCommunityAsync(string title)
    {
        var communityData = new
        {
            title,
            description = "",
            isReadOnly = false,
            accessSetting = 0,
            telegramEnabled = false
        };
        var response = await _httpClient.PostAsJsonAsync("/api/v1/communities", communityData);
        response.EnsureSuccessStatusCode();
        var community = await response.Content.ReadFromJsonAsync<CommunityResponse>();
        return community.Id;
    }
    
    private class CommunityResponse
    {
        public string Id { get; init; }
        public string Title { get; init; }
        public string Description { get; init; }
        public string LastActivity { get; init; }
        public int AccessSetting { get; init; }
        public bool IsReadOnly { get; init; }
        public bool IsMember { get; init; }
        public bool IsModerator { get; init; }
        public bool IsShared { get; init; }
        public bool TelegramEnabled { get; init; }
    }
}