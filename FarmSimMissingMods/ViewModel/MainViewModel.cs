using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

    #endregion

    #region Events

    public event EventHandler ExitRequestedEvent;

    #endregion

    #region Methods

    private void RequestExit()
    {
        ExitRequestedEvent?.Invoke(this, EventArgs.Empty);
    }

    private void OnDownloadMissingMods(object obj)
    {
        var sourcePath = m_Config["ftp:server"] + m_Config["ftp:mods-directory"];
        var localModDirectory = m_Config["mods-directory"] ?? "";

            foreach (var serverMod in ServerMods)
            {
                if (serverMod.State == ModState.NotUpToDate || serverMod.State == ModState.Missing)
                {
                    Task.Run(() =>
                    {
                        using var fileStream = m_ModsServerDataAccess.DownloadFile(sourcePath + serverMod.Name + ".zip");

                        var outFilename = Path.Combine(localModDirectory, serverMod.Name + ".zip");
                        Console.WriteLine("Save file to " + outFilename);
                        using var outFileStream = File.Create(outFilename);
                        
                        fileStream.CopyTo(outFileStream);
                        
                        serverMod.LocalHash = serverMod.Hash;
                        serverMod.State = ModState.UpToDate;
                    });
                }
            }
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
        ServerMods.Clear();
        var serverMods = await m_ServerStatDataAccess.GetMods();
        
        foreach (var serverMod in serverMods)
        {
            ServerMods.Add(serverMod);
        }
    }

    private void ValidateLocalMods()
    {
        Task.Run(() =>
        {
            var modsDirectory = m_Config["mods-directory"] ?? "";

            foreach (var serverMod in ServerMods)
            {
                var filenameWithExtension = serverMod.Name + ".zip";
                var filename = Path.Combine(modsDirectory, filenameWithExtension);
                if (!File.Exists(filename))
                {
                    serverMod.State = ModState.Missing;
                    serverMod.LocalHash = "Not found";
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