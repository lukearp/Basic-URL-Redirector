using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Azure.Identity;

namespace Basic_URL_Redirector.Data;

public class AppSettingsData
{
    public FileData fileData { get; set; }
    public StorageAccountInfo storageAccountInfo { get; set; }
    public int refreshInMintues {get;set;}
}

public class FileData
{
    public string masterDefault {get;set;}
    public List<RedirectDefaults> hostnames { get; set; }
}

public class RedirectDefaults
{
    public string hostname { get; set; }
    public string defaultTarget { get; set; }
}

public class StorageAccountInfo
{
    public string storageAccountContainerUrl { get; set; }
    public string storageAccountConnectionString {get;set;}
    public string storageAccountContainer {get;set;}

    public StorageAccountInfo()
    {
        storageAccountContainerUrl = "";
        storageAccountConnectionString = "";
        storageAccountContainer = "";
    }
}
public class HostnameRedirects
{
    public string hostname { get; set; }
    public UrlRedirects urlRedirects { get; set; }
    public string defaultTarget {get;set;}
}
public class UrlRedirects
{
    public List<UrlRedirect> redirects { get; set; }

    public UrlRedirects()
    {
        redirects = new List<UrlRedirect>();
    }
}

public class UrlRedirect
{
    public string source { get; set; }
    public string target { get; set; }
}

public class DataLoad : BackgroundService, IDisposable
{
    private Timer _timer;
    private readonly ILogger<DataLoad> _logger;
    private readonly FileData _fileData;
    public static string masterDefault {get;set;}
    private readonly StorageAccountInfo _storageAccountInfo;
    private static BlobContainerClient blobContainerClient { get; set; }
    private static List<HostnameRedirects> redirectMaps { get; set; }
    private static int refreshInMintues {get;set;}
    private BlobClient blobClient { get; set; }

    public DataLoad(IOptions<AppSettingsData> appSettings, ILogger<DataLoad> logger)
    {
        _logger = logger;
        _fileData = appSettings.Value.fileData;
        _storageAccountInfo = appSettings.Value.storageAccountInfo;
        if(_storageAccountInfo.storageAccountConnectionString != ""){
            blobContainerClient = new BlobContainerClient(_storageAccountInfo.storageAccountConnectionString,_storageAccountInfo.storageAccountContainer);
        }
        else {
            blobContainerClient = new BlobContainerClient(new Uri(_storageAccountInfo.storageAccountContainerUrl), new DefaultAzureCredential());
        }
        redirectMaps = new List<HostnameRedirects>();
        masterDefault = _fileData.masterDefault;
        refreshInMintues = appSettings.Value.refreshInMintues;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            LoadData();

            await Task.Delay(TimeSpan.FromMinutes(refreshInMintues), stoppingToken); // waits 1 second
        }
    }

    private void LoadData()
    {
        List<HostnameRedirects> thisRedirectList = new List<HostnameRedirects>();
        foreach (RedirectDefaults hostname in _fileData.hostnames)
        {
            _logger.LogInformation("Hostname: {Hostname}, TimeStamp: {TimeStamp}",[hostname.hostname, DateTime.Now.ToString()]);
            HostnameRedirects thisHost = new HostnameRedirects() { hostname = hostname.hostname, defaultTarget = hostname.defaultTarget};
            blobClient = blobContainerClient.GetBlobClient(hostname.hostname + ".json");
            try {
                Stream blobStream = blobClient.OpenRead();
                using (MemoryStream ms = new MemoryStream())
                {
                    blobStream.CopyTo(ms);
                    ms.Position = 0;
                    using (StreamReader reader = new StreamReader(ms))
                    {
                        string text = reader.ReadToEnd();
                        thisHost.urlRedirects = JsonConvert.DeserializeObject<UrlRedirects>(text);
                    }
                }
                thisRedirectList.Add(thisHost);
            } catch {
               _logger.LogInformation(hostname.hostname + ".json not Found"); 
            }
        }
        redirectMaps = thisRedirectList;
    }

    public static string GetRedirect(string hostname, string path)
    {
        string otherPath = "";
        string? trimmedSource = "";
        if(path.EndsWith("/"))
        {
            otherPath = path.TrimEnd('/');
        }
        else
        {
            otherPath = path + "/";
        }
        HostnameRedirects? redirect = redirectMaps.Find(x => x.hostname.ToLower() == hostname.ToLower());
        if(redirect != null)
        {
           UrlRedirect? targetRedirect = null;
           for(int i = 0; i < redirect.urlRedirects.redirects.Count; i++)
           {
                try {
                    trimmedSource = redirect.urlRedirects.redirects[i].source.TrimEnd();
                }
                catch {
                    trimmedSource = "";
                }
                //Console.WriteLine(redirect.urlRedirects.redirects[i].source + "\n" + trimmedSource);
                if(String.Equals(trimmedSource, path, StringComparison.OrdinalIgnoreCase) || String.Equals(trimmedSource, otherPath, StringComparison.OrdinalIgnoreCase))
                {
                    targetRedirect = redirect.urlRedirects.redirects[i];
                    break;
                }
           }
           if(targetRedirect == null)
           {
                return redirect.defaultTarget;
           }
           else
           {
                return targetRedirect.target;
           }
        }
        else
        {
            return DataLoad.masterDefault;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // This method is called when the application is stopping.
        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}