namespace FarmSimMissingMods.Model;

public class Mod : ObservableObject
{
    #region Fields

    private ModState m_ModState;
    private string m_LocalHash;
    private string m_Hash;
    #endregion

    #region Constructors

    public Mod(string name, string label, string author, string version, string hash)
    {
        Name = name;
        Label = label;
        Author = author;
        Version = version;
        Hash = hash;
    }

    #endregion

    #region Properties
    
    public string Name { get; }
    public string Label { get; }
    public string Author { get; }
    public string Version { get; }
    public string Hash {
        get => m_Hash;
        set
        {
            if (m_Hash != value)
            {
                m_Hash = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HashAreEquals));
            }
        }
    }
    public ModState State {
        get => m_ModState;
        set
        {
            if (m_ModState != value)
            {
                m_ModState = value;
                OnPropertyChanged();
            }
        }
    }
    public string LocalHash {
        get => m_LocalHash;
        set
        {
            if (m_LocalHash != value)
            {
                m_LocalHash = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HashAreEquals));
            }
        }
    }

    public bool HashAreEquals => string.IsNullOrEmpty(LocalHash) || Hash.Equals(LocalHash);
    
    #endregion

    #region Methods

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(Label)}: {Label}, {nameof(Author)}: {Author}, {nameof(Version)}: {Version}, {nameof(Hash)}: {Hash}, {nameof(State)}: {State}, {nameof(LocalHash)}: {LocalHash}, {nameof(HashAreEquals)}: {HashAreEquals}";
    }

    #endregion
}