using System.Globalization;
using System.Resources;
using FarmSimMissingMods.Resources.I18N;

namespace FarmSimMissingMods.Model.I18N;

public class I18NManager
{
    #region Fields

    private CultureInfo m_CultureInfo;
    private static readonly CultureInfo DefaultCultureInfo = new CultureInfo("en");
    private readonly ResourceManager m_ResourceManager;
    private static readonly I18NManager Instance = new I18NManager();

    #endregion

    #region Constructors

    private I18NManager()
    {
        m_CultureInfo = Thread.CurrentThread.CurrentCulture;
        m_ResourceManager = new ResourceManager("FarmSimMissingMods.Resources.I18N.L10N", typeof(L10N).Assembly);
    }

    #endregion
    
    #region Static Properties

    public static event EventHandler CultureInfoChanged;
    
    #endregion

    #region Properties
    
    public static CultureInfo CultureInfo
    {
        get => Instance.m_CultureInfo;
        set
        {
            if (!Equals(Instance.m_CultureInfo, value))
            {
                Instance.m_CultureInfo = value;
                Thread.CurrentThread.CurrentCulture = value;
                Thread.CurrentThread.CurrentUICulture = value;
                
                CultureInfoChanged?.Invoke(null, EventArgs.Empty);
            }
        }
    }
    
    #endregion

    #region Methods

    public static string GetTranslation(string key, CultureInfo cultureInfo, params object[] args)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }
        
        var value = Instance.m_ResourceManager.GetString(key, cultureInfo);

        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }
        
        value = Instance.m_ResourceManager.GetString(key, DefaultCultureInfo);

        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        return "unknown_translation_" + key;
    }
    
    public static string GetTranslation(string key)
    {
        return GetTranslation(key, Instance.m_CultureInfo);
    }
    
    public static string GetTranslation(string key, params object[] args)
    {
        return string.Format(GetTranslation(key, Instance.m_CultureInfo), args);
    }
    #endregion
}