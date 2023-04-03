using System;
using FarmSimMissingMods.Model;

namespace FarmSimMissingMods.ViewModel
{
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        public abstract void Dispose();
    }
}