using System.Collections.ObjectModel;
using FarmSimMissingMods.Model;

namespace FarmSimMissingMods.DataAccess;

public interface IServerStatDataAccess
{
    public Task<ObservableCollection<Mod>> GetMods();
}