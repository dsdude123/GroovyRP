﻿using GroovyRP;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace GroovyRPHelper
{
    static class Program
    {

        static Process trayKillabaleCore = new Process();
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Process[] coreProcesses = Process.GetProcessesByName("GroovyRPCore");
            if (coreProcesses.Length > 0)
            {
                Environment.Exit(0); // GroovyRP already running.
            }

            Config myConfig = LoadConfiguration();
            if(myConfig.RunWhenGrooveMusicOpens)
            {
                Process[] grooveMusicInstances = Process.GetProcessesByName("Music.UI");
                if (grooveMusicInstances.Length > 0)
                {
                    Process grooveMusic = grooveMusicInstances[0];
                    if ((grooveMusic.Threads[0].ThreadState == ThreadState.Wait) && grooveMusic.Threads[0].WaitReason == ThreadWaitReason.Suspended)
                    {
                        // Some times Groove Music will start up suspended in the background with Suspended state and generate an event. 
                        // Ok to close GroovyRP since user did not open Groove Music.
                        Environment.Exit(0);
                    }
                }
            }

            string corePath = Path.GetDirectoryName(Application.ExecutablePath) + @"\GroovyRPCore.exe";
            if (myConfig.HideInSystemTray)
            {
                ProcessStartInfo hiddenStart = new ProcessStartInfo();
                hiddenStart.FileName = corePath;
                hiddenStart.WindowStyle = ProcessWindowStyle.Hidden;
                hiddenStart.CreateNoWindow = true;
                Process coreProcess = Process.Start(hiddenStart);
                NotifyIcon systemTray = new NotifyIcon();
                systemTray.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                systemTray.Text = "GroovyRP";

                ContextMenu contextMenu = new ContextMenu();
                MenuItem recongifureOption = new MenuItem();
                MenuItem exitOption = new MenuItem();

                contextMenu.MenuItems.AddRange(new MenuItem[] { recongifureOption, exitOption });

                recongifureOption.Index = 0;
                recongifureOption.Text = "Reconfigure";
                recongifureOption.Click += new EventHandler(Reconfigure);

                exitOption.Index = 1;
                exitOption.Text = "Exit";
                exitOption.Click += new EventHandler(ExitProgram);

                systemTray.ContextMenu = contextMenu;
                systemTray.Visible = true;
                trayKillabaleCore = coreProcess;
                while(!coreProcess.HasExited)
                {
                    Application.DoEvents();
                }
                Environment.Exit(0);
            } else
            {
                Process.Start(corePath);
                Environment.Exit(0);
            }
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
                    MessageBox.Show("An error occured loading the configuration file.", "Invalid Configuration File", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    task.Principal.RunLevel = TaskRunLevel.Highest;


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

        public static void ExitProgram(object Sender, EventArgs e)
        {
            trayKillabaleCore.Kill();
            Environment.Exit(0);
        }
    }
}