using GroovyRP;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace GroovyRPHelper
{
    static class Program
    {

        static Process coreProcess = new Process();
        static NotifyIcon systemTray = new NotifyIcon();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Process[] helperProcesses = Process.GetProcessesByName("GroovyRP");
            if (helperProcesses.Length > 1)
            {
                Environment.Exit(0); // GroovyRP already running.
            } else
            {
                Process[] coreProcesses = Process.GetProcessesByName("GroovyRPCore");
                foreach(Process process in coreProcesses)
                {
                    process.Kill(); // Make sure we don't have multiple cores running
                }
            }

            Config myConfig = LoadConfiguration();

            string corePath = Path.GetDirectoryName(Application.ExecutablePath) + @"\GroovyRPCore.exe";
            if (myConfig.HideInSystemTray)
            {
                ProcessStartInfo hiddenStart = new ProcessStartInfo();
                hiddenStart.FileName = corePath;
                hiddenStart.WindowStyle = ProcessWindowStyle.Hidden;
                hiddenStart.CreateNoWindow = true;
                coreProcess = Process.Start(hiddenStart);                          
            } else
            {
                coreProcess = Process.Start(corePath);
            };

            systemTray.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            systemTray.Text = "GroovyRP";

            ContextMenu contextMenu = new ContextMenu();
            MenuItem recongifureOption = new MenuItem();
            MenuItem updateOption = new MenuItem();
            MenuItem exitOption = new MenuItem();

            contextMenu.MenuItems.AddRange(new MenuItem[] { recongifureOption, updateOption, exitOption });

            recongifureOption.Index = 0;
            recongifureOption.Text = "Reconfigure";
            recongifureOption.Click += new EventHandler(Reconfigure);

            updateOption.Index = 1;
            updateOption.Text = "Check for Updates";
            updateOption.Click += new EventHandler(UpdateCheck);

            exitOption.Index = 2;
            exitOption.Text = "Exit";
            exitOption.Click += new EventHandler(ExitProgram);

            systemTray.ContextMenu = contextMenu;
            systemTray.Visible = true;
            
            if(myConfig.AutoCheckForUpdates) UpdateCheck(null, null);

            while (!coreProcess.HasExited)
            {
                checkGrooveMusicStatus(); // Don't allow program to keep running if Groove Music isn't running
                Application.DoEvents();
            }
            Environment.Exit(0);
        }

        private static Config LoadConfiguration()
        {
            string path = "Config.xml";
            XmlSerializer serializer = new XmlSerializer(typeof(Config));
            if (File.Exists(path))
            {
                try
                {
                    StreamReader reader = new StreamReader(path);
                    Config existingConfig = (Config)serializer.Deserialize(reader);
                    reader.Close();

                    if (!existingConfig.IsFirstRun)
                    {
                        return existingConfig;
                    } 
                }
                catch
                {
                    MessageBox.Show("An error occurred loading the configuration file.", "Invalid Configuration File", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("The configuration file is missing. Starting first run setup.", "Missing Configuration File", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Config config = CreateConfiguration();

            FileStream newConfig = new FileStream("Config.xml", FileMode.Create);
            serializer.Serialize(newConfig, config);
            newConfig.Close();
            MessageBox.Show("First run setup is complete.", "First Run Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return config;
        }

        private static void RegisterTask()
        {
            using (TaskService taskService = new TaskService())
            {
                if (taskService.FindTask("GroovyRPAutoStart") == null)
                {
                    TaskDefinition task = taskService.NewTask();
                    task.RegistrationInfo.Author = "C.S. Media";
                    task.RegistrationInfo.Description = "Automatically opens GroovyRP when Groove Music is opened.";

                    EventTrigger startupTrigger = new EventTrigger();
                    startupTrigger.Subscription = @"
                        <QueryList>
                            <Query Id='0' Path='Application'>
                            <Select Path='Application'>Event[System[Provider[@Name='ESENT'] and (Level=4 or Level=0) and (EventID=102)] and EventData[Data='Music.UI']]</Select>
                            </Query>
                        </QueryList>
                        ";

                    task.Triggers.Add(startupTrigger);
                    task.Actions.Add(new ExecAction(Application.ExecutablePath, null, Path.GetDirectoryName(Application.ExecutablePath)));

                    taskService.RootFolder.RegisterTaskDefinition(@"GroovyRPAutoStart", task);
                }
            }
        }

        private static void UnregisterTask()
        {
            using (TaskService taskService = new TaskService())
            {
                if (!(taskService.FindTask("GroovyRPAutoStart") == null))
                {
                    taskService.RootFolder.DeleteTask("GroovyRPAutoStart");
                }
            }
        }

        private static Config CreateConfiguration()
        {
            Config config = new Config();
            DialogResult autoStartChoice = MessageBox.Show("Do you want to automatically start GroovyRP when Groove Music is opened?", "First Run Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (autoStartChoice.Equals(DialogResult.Yes))
            {
                RegisterTask();
                config.RunWhenGrooveMusicOpens = true;
            }
            else
            {
                UnregisterTask();
                config.RunWhenGrooveMusicOpens = false;
            }

            DialogResult hideChoice = MessageBox.Show("Do you want to run GroovyRP in the background when it is opened?", "First Run Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (hideChoice.Equals(DialogResult.Yes))
            {
                config.HideInSystemTray = true;
            }
            else
            {
                config.HideInSystemTray = false;
            }

            DialogResult updateChoice = MessageBox.Show("Do you want to automatically check for updates?", "First Run Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (updateChoice.Equals(DialogResult.Yes))
            {
                config.AutoCheckForUpdates = true;
            }
            else
            {
                config.AutoCheckForUpdates = false;
            }

            config.IsFirstRun = false;

            return config;
        }

        private static void Reconfigure(object Sender, EventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Config));
            Config config = CreateConfiguration();
            FileStream newConfig = new FileStream("Config.xml", FileMode.Create);
            serializer.Serialize(newConfig, config);
            newConfig.Close();
            MessageBox.Show("First run setup is complete.", "First Run Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void UpdateCheck(object Sender, EventArgs e)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(VersionInfo));
                    Stream dataStream = client.OpenRead(@"https://raw.githubusercontent.com/dsdude123/GroovyRP/master/VersionInfo.xml");
                    VersionInfo newestVersion = (VersionInfo)serializer.Deserialize(dataStream);
                    Version currentVersion = Assembly.GetEntryAssembly().GetName().Version;

                    if (new Version(newestVersion.Version) > currentVersion)
                    {
                        systemTray.BalloonTipIcon = ToolTipIcon.Info;
                        systemTray.BalloonTipTitle = "New Update Available";
                        systemTray.BalloonTipText = $"A new update for GroovyRP is available. Version {newestVersion.Version} is available. You have version {currentVersion}.";
                        systemTray.BalloonTipClicked += new EventHandler(UpdateNotificationClicked);
                        systemTray.ShowBalloonTip(5000);
                    }
                }
            }
            catch { }
        }

        public static void checkGrooveMusicStatus()
        {
            Process[] grooveMusics = Process.GetProcessesByName("Music.UI");
            if (grooveMusics.Length < 1)
            {
                Environment.Exit(0);
            }
            else if (grooveMusics.Length > 0)
            {
                try
                {
                    Process grooveMusic = grooveMusics[0];
                    bool areThreadsActive = false;

                    for (int i = 0; i < grooveMusic.Threads.Count; i++)
                    {
                        if (!grooveMusic.Threads[i].ThreadState.Equals(ThreadState.Terminated) &&
                        !(grooveMusic.Threads[i].ThreadState.Equals(ThreadState.Wait) && grooveMusic.Threads[i].WaitReason.Equals(ThreadWaitReason.Suspended)))
                        {
                            areThreadsActive = true;
                        }
                    }

                    if (!areThreadsActive)
                    {
                        ExitProgram(null, null);
                    }
                } catch (InvalidOperationException)
                {
                    
                }
            }
        }

        private static void UpdateNotificationClicked(object Sender, EventArgs e)
        {
            Process.Start(@"https://github.com/dsdude123/GroovyRP/releases/latest");
        }

        public static void ExitProgram(object Sender, EventArgs e)
        {
            coreProcess.Kill();
            Environment.Exit(0);
        }
    }
}
