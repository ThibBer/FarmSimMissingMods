using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Media;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FarmSimMissingMods.DataAccess;
using FarmSimMissingMods.Model;
using FarmSimMissingMods.Model.Command;
using FarmSimMissingMods.Model.Exceptions;
using log4net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Yaml;

namespace FarmSimMissingMods.ViewModel;

public class MainViewModel : ViewModelBase
{
    #region Fields

    private readonly ILog m_Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    
    private IServerStatDataAccess m_ServerStatDataAccess;
    private IModsServerDataAccess m_ModsServerDataAccess;
    private IConfigurationRoot m_Config;
    private MD5 m_MD5;
    private byte[] m_BufferByteArray;
    private bool m_IsBusy;
    private int m_InvalidModsCount;
    private SoundPlayer m_SoundPlayer;
    private bool m_SoundPlayerIsActive;

    #endregion

    #region Constructors

    public MainViewModel()
    {
        var configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory());
        configurationBuilder.AddYamlFile("config.yml");

        m_Config = configurationBuilder.Build();
        m_MD5 = MD5.Create();

        RefreshServerModsCommand = new DelegateCommand(OnRefreshMods);
        ValidateLocalModsCommand = new DelegateCommand(OnValidateLocalMods);
        DownloadMissingModsCommand = new DelegateCommand(OnDownloadMissingMods);

        var server = m_Config["ftp:server"];
        if (string.IsNullOrEmpty(server))
        {
            m_Logger.Error("Invalid server address");
            MessageBox.Show("ERROR : Invalid server address", "Error occured", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            RequestExit();
        }

        var username = m_Config["ftp:username"];
        if (string.IsNullOrEmpty(username))
        {
            m_Logger.Error("Invalid ftp address");
            MessageBox.Show("ERROR : ftp username", "Error occured", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            RequestExit();
        }

        var password = m_Config["ftp:password"];
        if (string.IsNullOrEmpty(password))
        {
            m_Logger.Error("Invalid ftp password");
            MessageBox.Show("ERROR : ftp password", "Error occured", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            RequestExit();
        }

        var ftpModsDirectory = m_Config["ftp:mods-directory"];
        if (string.IsNullOrEmpty(ftpModsDirectory))
        {
            m_Logger.Error("Invalid ftp mods directory");
            MessageBox.Show("ERROR : ftp mods directory", "Error occured", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            RequestExit();
        }

        var serverIp = m_Config["server:ip"];
        if (string.IsNullOrEmpty(serverIp))
        {
            m_Logger.Error("Invalid server IP");
            MessageBox.Show("ERROR : Invalid server IP", "Error occured", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            RequestExit();
        }

        var serverCode = m_Config["server:code"];
        if (string.IsNullOrEmpty(serverCode))
        {
            m_Logger.Error("Invalid server code");
            MessageBox.Show("ERROR : Invalid server code", "Error occured", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            RequestExit();
        }

        var modsDirectory = m_Config["mods-directory"];
        if (string.IsNullOrEmpty(modsDirectory))
        {
            m_Logger.Error("Invalid mods directory");
            MessageBox.Show("ERROR : Invalid mods directory", "Error occured", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            RequestExit();
        }
        
        var musicStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FarmSimMissingMods.Resources.sound.wav");
        
        if (musicStream != null)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "FarmSimMissingMods");
            if (!Directory.Exists(tempDirectory))
            {
                Directory.CreateDirectory(tempDirectory);
            }

            var filename = Path.Combine(tempDirectory, "sound.mp3");
            
            using var file = new FileStream(filename, FileMode.Create, FileAccess.Write);
            musicStream.CopyTo(file);
            musicStream.Close();
            
            m_SoundPlayer = new SoundPlayer(filename);
            m_SoundPlayerIsActive = false;
        }

        if (!string.IsNullOrEmpty(username) && username.Contains("paulo5090r") && m_Config["troll"] == null)
        {
            StartTroll();
        }
        
        InvalidModsCount = 0;
        
        m_ServerStatDataAccess = new ServerStatXmlAccess(serverIp, serverCode);
        m_ModsServerDataAccess = new ModsServerFtpAccess(username, password);
        ServerMods = new ObservableCollection<Mod>();

        RefreshServerMods();
    }

    #endregion

    #region Properties

    #region Commands

    public ICommand RefreshServerModsCommand { get; }
    public ICommand ValidateLocalModsCommand { get; }
    public ICommand DownloadMissingModsCommand { get; }

    #endregion

    public ObservableCollection<Mod> ServerMods { get; set; }

    public bool IsBusy
    {
        get => m_IsBusy;
        set
        {
            if (m_IsBusy != value)
            {
                m_IsBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanDownloadMods));
            }
        }
    }
    public int InvalidModsCount
    {
        get => m_InvalidModsCount;
        set
        {
            if (m_InvalidModsCount != value)
            {
                m_InvalidModsCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanDownloadMods));
            }
        }
    }

    public bool CanDownloadMods => m_InvalidModsCount > 0 && !IsBusy;

    #endregion

    #region Events

    public event EventHandler ExitRequestedEvent;

    #endregion

    #region Methods

    private void StartTroll()
    {
        var result = MessageBox.Show(
            "Pour utiliser ce magnifique logiciel, tu dois confirmer que t'es vraiment une grosse salope, un énorme PD et que ThibBer est un vrai génie.\n" +
            "VIVE LAURIE, VIVE LISON, GODFERDEK VIVE POOOLLL LE PD :)", "Petite vérification avant de commencer ...", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            if (!m_SoundPlayerIsActive)
            {
                m_SoundPlayer.Play();
                m_SoundPlayerIsActive = true;
            }

            StartTroll();
        }
        else
        {
            if (m_SoundPlayerIsActive)
            {
                m_SoundPlayer.Stop();
                m_SoundPlayerIsActive = false;
            }
        }
    }

    private void RequestExit()
    {
        ExitRequestedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void OnDownloadMissingMods(object obj)
    {
        Task.Run(() =>
        {
            IsBusy = true;

            var sourcePath = m_Config["ftp:server"] + m_Config["ftp:mods-directory"];
            var localModDirectory = m_Config["mods-directory"] ?? "";

            foreach (var serverMod in ServerMods)
            {
                if (serverMod.State == ModState.NotUpToDate || serverMod.State == ModState.Missing)
                {
                    using var fileStream = m_ModsServerDataAccess.DownloadFile(sourcePath + serverMod.Name + ".zip");

                    var outFilename = Path.Combine(localModDirectory, serverMod.Name + ".zip");
                    using var outFileStream = File.Create(outFilename);
                    
                    fileStream.CopyTo(outFileStream);
                    
                    serverMod.LocalHash = serverMod.Hash;
                    serverMod.State = ModState.UpToDate;

                    if (InvalidModsCount - 1 >= 0)
                    {
                        InvalidModsCount--;
                    }
                }
            }

            MessageBox.Show("Mods are now up-to-date !", "Job is done", MessageBoxButton.OK);

            IsBusy = false;
        });
    }

    private void OnRefreshMods(object obj)
    {
        RefreshServerMods();
    }

    private void OnValidateLocalMods(object obj)
    {
        ValidateLocalMods();
    }

    private async void RefreshServerMods()
    {
        IsBusy = true;
        ServerMods.Clear();

        try
        {
            var serverMods = await m_ServerStatDataAccess.GetMods();

            if (serverMods.Count == 0)
            {
                const string information = "You server is off or no mods are loaded in the game !";

                MessageBox.Show(information, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                m_Logger.Info(information);

                return;
            }

            foreach (var serverMod in serverMods)
            {
                ServerMods.Add(serverMod);
            }
        }
        catch (FetchServerStatsException e)
        {
            const string error = "Can't fetch server mods data. Maybe the server is unavailable. Start it or try to fix the server error";

            MessageBox.Show(error, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
            m_Logger.Error(error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ValidateLocalMods()
    {
        Task.Run(() =>
        {
            InvalidModsCount = 0;
            IsBusy = true;
            var modsDirectory = m_Config["mods-directory"] ?? "";

            foreach (var serverMod in ServerMods)
            {
                var filenameWithExtension = serverMod.Name + ".zip";
                var filename = Path.Combine(modsDirectory, filenameWithExtension);
                if (!File.Exists(filename))
                {
                    serverMod.State = ModState.Missing;
                    serverMod.LocalHash = "Not found";
                    InvalidModsCount++;
                }
                else
                {
                    try
                    {
                        GetBytes(filename, serverMod.Name, out m_BufferByteArray);
                        var localFileHash = BitConverter.ToString(m_MD5.ComputeHash(m_BufferByteArray)).Replace("-", string.Empty).ToLower();
                        serverMod.LocalHash = localFileHash;

                        if (!localFileHash.Equals(serverMod.Hash))
                        {
                            serverMod.State = ModState.NotUpToDate;
                            InvalidModsCount++;
                        }
                        else
                        {
                            serverMod.State = ModState.UpToDate;
                        }
                    }
                    catch (Exception e)
                    {
                        serverMod.State = ModState.Error;
                        
                        m_Logger.ErrorFormat("Error while trying to generate ({0}) hash !", serverMod);
                        m_Logger.Error(e);
                        Console.WriteLine(e);
                    }
                }

                GC.Collect();
            }

            IsBusy = false;
        });
    }

    private void GetBytes(string filename, string name, out byte[] biteArray)
    {
        var second = Encoding.ASCII.GetBytes(name);

        var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16 * 4096 * 4096);
        biteArray = new byte[fs.Length + second.Length];

        fs.Read(biteArray, 0, Convert.ToInt32(fs.Length));
        Buffer.BlockCopy(second, 0, biteArray, (int) fs.Length, second.Length);


        fs.Close();
    }

    public override void Dispose()
    {
        m_MD5?.Dispose();
        m_SoundPlayer?.Dispose();
    }

    #endregion
}