namespace FarmSimMissingMods.DataAccess;

public interface IModsServerDataAccess
{
    public Stream DownloadFile(string source);
}