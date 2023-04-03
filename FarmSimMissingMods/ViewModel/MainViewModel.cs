using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Media;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FarmSimMissingMods.DataAccess;
using FarmSimMissingMods.Model;
using FarmSimMissingMods.Model.Command;
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
    private Thread m_Thread;
    private int m_InvalidModsCount;

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
            MessageBox.Show("ERROR : Invalid server address", "Error occured");
            RequestExit();
        }

        var username = m_Config["ftp:username"];
        if (string.IsNullOrEmpty(username))
        {
            m_Logger.Error("Invalid ftp address");
            MessageBox.Show("ERROR : ftp username", "Error occured");
            RequestExit();
        }

        var password = m_Config["ftp:password"];
        if (string.IsNullOrEmpty(password))
        {
            m_Logger.Error("Invalid ftp password");
            MessageBox.Show("ERROR : ftp password", "Error occured");
            RequestExit();
        }

        var ftpModsDirectory = m_Config["ftp:mods-directory"];
        if (string.IsNullOrEmpty(ftpModsDirectory))
        {
            m_Logger.Error("Invalid ftp mods directory");
            MessageBox.Show("ERROR : ftp mods directory", "Error occured");
            RequestExit();
        }

        var serverIp = m_Config["server:ip"];
        if (string.IsNullOrEmpty(serverIp))
        {
            m_Logger.Error("Invalid server IP");
            MessageBox.Show("ERROR : Invalid server IP", "Error occured");
            RequestExit();
        }

        var serverCode = m_Config["server:code"];
        if (string.IsNullOrEmpty(serverCode))
        {
            m_Logger.Error("Invalid server code");
            MessageBox.Show("ERROR : Invalid server code", "Error occured");
            RequestExit();
        }

        var modsDirectory = m_Config["mods-directory"];
        if (string.IsNullOrEmpty(modsDirectory))
        {
            m_Logger.Error("Invalid mods directory");
            MessageBox.Show("ERROR : Invalid mods directory", "Error occured");
            RequestExit();
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
            "VIVE LAURIE, VIVE LISON, GODFERDEK VIVE POOOLLL LE PD :)", "Petite vérification avant de commencer ...", MessageBoxButton.YesNoCancel);

        if (result != MessageBoxResult.Yes)
        {
            m_Thread = new Thread(() =>
            {
                var i = 0;
                while (i < 5)
                {
                    SystemSounds.Exclamation.Play();
                    Thread.Sleep(500);

                    i++;
                }
            });
            
            m_Thread.Start();
            
            StartTroll();
        }
    }

    private void RequestExit()
    {
        ExitRequestedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void OnDownloadMissingMods(object obj)
    {
        IsBusy = true;
        
        Task.Run(() =>
        {
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
        });

        IsBusy = false;
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
        var serverMods = await m_ServerStatDataAccess.GetMods();
        
        foreach (var serverMod in serverMods)
        {
            ServerMods.Add(serverMod);
        }

        IsBusy = false;
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
    }

    #endregion
}