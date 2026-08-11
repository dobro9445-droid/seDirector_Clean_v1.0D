using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace seDirector.Core;

public sealed class UpdateService
{
    private readonly Logger _logger;
    private readonly HttpClient _httpClient;
    
    // ВАЖНО: Укажите ваш логин GitHub и точное название репозитория
    private const string RepoOwner = "dobro9445-droid";
    private const string RepoName = "seDirector_Clean_v1.0";
    
    // Текущая версия программы
    private const string CurrentVersion = "1.4.0";

    public UpdateService(Logger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "seDirector-Clean");
    }

    public async Task<string> CheckForUpdatesAsync()
    {
        try
        {
            var url = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning("Проверка обновлений: не удалось получить данные от GitHub.");
                return "Не удалось проверить обновления. Убедитесь, что в репозитории есть хотя бы один релиз.";
            }

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var release = JsonSerializer.Deserialize<GitHubRelease>(json, options);

            if (release == null)
                return "Не удалось разобрать ответ от GitHub.";

            var latestVersion = release.TagName.TrimStart('v');
            
            if (IsNewerVersion(latestVersion, CurrentVersion))
            {
                var message = "========================================\n" +
                              "  ДОСТУПНА НОВАЯ ВЕРСИЯ!\n" +
                              "========================================\n" +
                              $"Текущая версия: {CurrentVersion}\n" +
                              $"Новая версия:   {latestVersion}\n" +
                              $"Название:       {release.Name}\n" +
                              $"Скачать здесь:  {release.HtmlUrl}\n" +
                              "========================================";
                _logger.Info("Найдено обновление до версии " + latestVersion);
                return message;
            }
            else
            {
                _logger.Info("Установлена последняя версия.");
                return "У вас установлена последняя версия (" + CurrentVersion + ").";
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Ошибка проверки обновлений: " + ex.Message);
            return "Ошибка проверки обновлений: " + ex.Message;
        }
    }

    private bool IsNewerVersion(string latest, string current)
    {
        if (Version.TryParse(latest, out var latestVer) && Version.TryParse(current, out var currentVer))
        {
            return latestVer > currentVer;
        }
        return string.Compare(latest, current, StringComparison.Ordinal) > 0;
    }
}

public class GitHubRelease
{
    public string TagName { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
