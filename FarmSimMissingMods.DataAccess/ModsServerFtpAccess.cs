using System.Net;

namespace FarmSimMissingMods.DataAccess;

public class ModsServerFtpAccess : IModsServerDataAccess
{
    #region Fields
    

    #endregion

    #region Constructors

    public ModsServerFtpAccess(string username, string password)
    {
        Username = username;
        Password = password;
    }

    #endregion

    #region Properties
    private string Username { get; }
    private string Password { get; }
    #endregion

    #region Methods

    public Stream DownloadFile(string source)
    {
        var request = (FtpWebRequest) WebRequest.Create("ftp://" + source);  
        request.Method = WebRequestMethods.Ftp.DownloadFile;  

        request.Credentials = new NetworkCredential(Username, Password);  
        var response = (FtpWebResponse)request.GetResponse();  
        return response.GetResponseStream();  
    }

    #endregion
}