using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordRPC;
using CSCore;
using CSCore.CoreAudioAPI;
using TagLib;

namespace GroovyRP
{
    class Program
    {
        public static DiscordRpcClient client;

        static void Main(string[] args)
        {
            Console.WriteLine("GroovyRP\nhttps://github.com/dsdude123/GroovyRP\n\n");
            string appkey = "";
            // Get application key
            client = new DiscordRpcClient(appkey);
            client.Initialize();
            while (true)
            {
                if (audioCheck())
                { 
                    Console.Clear();
                    Console.WriteLine("GroovyRP\nhttps://github.com/dsdude123/GroovyRP\n\n");
                    // 
                    Process handlefinder = Process.Start("OpenedFilesView.exe", "/processfilter Music.UI.exe /scomma files.csv");
                    handlefinder.WaitForExit();
                    string csvcontents = System.IO.File.ReadAllText("files.csv");
                    if (csvcontents.Equals(""))
                    {
                        goto skip;
                    }
                    string[] rows = csvcontents.Split('\n');
                    foreach (string c in rows)
                    {
                        if (!c.Equals(""))
                        {

                            string[] data = c.Split(',');
                            if (data[22].Equals("m4a"))
                            {
                                Console.WriteLine("Detected: " + data[1]);
                                var media = TagLib.File.Create(data[1]);
                                Console.WriteLine("Title: " + media.Tag.Title);
                                //Console.WriteLine("Artist: " + media.Tag.AlbumArtists[0]);
                                //Console.WriteLine("Album: " + media.Tag.Album);
                                Console.WriteLine(
                                    "==============================================================================================================");
                            }
                        }
                    }
                    skip:
                    System.Threading.Thread.Sleep(20000);
                }
            }

        }

        static bool audioCheck()
        {
            if (Process.GetProcessesByName("Music.UI.exe").Length > 0)
            {
                AudioSessionManager2 sessionManager;
                using (var enumerator = new MMDeviceEnumerator())
                {
                    using (var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia))
                    {
                        sessionManager = AudioSessionManager2.FromMMDevice(device);
                    }
                }

                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        if (session.DisplayName.Equals("Groove Music"))
                        {
                            using (var audioMeter= session.QueryInterface<AudioMeterInformation>())
                            {
                                if (audioMeter.GetPeakValue() > 0)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }

                return false;
            }
            else
            {
                return false;
            }
        }
    }
}
