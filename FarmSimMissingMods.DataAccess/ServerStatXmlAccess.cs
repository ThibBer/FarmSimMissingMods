using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml;
using FarmSimMissingMods.Model;
using FarmSimMissingMods.Model.Exceptions;

namespace FarmSimMissingMods.DataAccess;

public class ServerStatXmlAccess : IServerStatDataAccess
{
    #region Fields

    private const int API_TIMEOUT_SECONDS = 5;
    
    private const string SERVER_STATS_ROUTE = "/feed/dedicated-server-stats.xml?code=";
    private HttpClient m_HttpClient;
    private string m_IpAddress;
    private string m_Code;

    #endregion

    #region Constructors

    public ServerStatXmlAccess(string ipAddress, string code)
    {
        m_HttpClient = new HttpClient();
        m_HttpClient.DefaultRequestHeaders.Add("User-Agent", "FarmSimMissingMods");
        m_HttpClient.Timeout = TimeSpan.FromSeconds(API_TIMEOUT_SECONDS);
        
        m_IpAddress = ipAddress;
        m_Code = code;
    }

    #endregion

    #region Properties

    #endregion

    #region Methods

    public async Task<ObservableCollection<Mod>> GetMods()
    {
        try
        {
            var url = m_IpAddress + SERVER_STATS_ROUTE + m_Code;
            await using var stream = await m_HttpClient.GetStreamAsync(url);
            using var streamReader = new StreamReader(stream);

            var doc = new XmlDocument();
            doc.LoadXml(await streamReader.ReadToEndAsync());

            var mods = new ObservableCollection<Mod>();
            var nodes = doc.SelectNodes("//Server/Mods/Mod");

            if (nodes != null)
            {
                foreach (XmlElement node in nodes)
                {
                    var name = node.GetAttribute("name");
                    var author = node.GetAttribute("author");
                    var version = node.GetAttribute("version");
                    var hash = node.GetAttribute("hash");

                    var mod = new Mod(name, node.InnerText, author, version, hash);
                    mods.Add(mod);
                }
            }

            return mods;
        }
        catch (HttpRequestException e)
        {
            throw new FetchServerStatsException("Server error occurs while fetching server stats", e);
        }
        catch (TaskCanceledException e)
        {
            throw new FetchServerStatsException("Fetching server stats task cancelled", e);
        }
        catch (Exception e)
        {
            throw new FetchServerStatsException(e.Message, e);
        }
    }

    #endregion
}