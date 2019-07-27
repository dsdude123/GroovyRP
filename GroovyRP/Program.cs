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
            client = new DiscordRpcClient("553434642766626861");
            client.Initialize();
            while (true)
            {
                if (audioCheck())
                {
                    
                    Console.Clear();
                    Console.WriteLine("GroovyRP\nhttps://github.com/dsdude123/GroovyRP\n\n");
                    // Get file handles
                    Process handlefinder = Process.Start("openedfilesview\\OpenedFilesView.exe", "/processfilter Music.UI.exe /scomma files.csv");
                    handlefinder.WaitForExit();
                    string csvcontents = System.IO.File.ReadAllText("files.csv");
                    if (csvcontents.Equals(""))
                    {
                        client.SetPresence(new RichPresence()
                        {
                            Details = "Failed to get track info"
                        });
                        Console.WriteLine("Failed to get track info");
                        client.Invoke();
                        System.Threading.Thread.Sleep(20000);
                    }
                    else
                    {
                        string[] rows = csvcontents.Split('\n');

                        string title = "Unknown Title";
                        string artist = "Unknown Artist";
                        string album = "Unknown Album";

                        foreach (string c in rows)
                        {
                            if (!c.Equals(""))
                            {
                                string[] data = c.Split(',');
                                if (supportedFileTypes.Contains(data[22], StringComparer.OrdinalIgnoreCase))
                                {

                                    var media = TagLib.File.Create(data[1]);
                                    title = media.Tag.Title;
                                    if (media.Tag.Artists.Length > 0)
                                    {
                                        artist = media.Tag.Artists.First();
                                    }
                                    else
                                    {
                                        if (media.Tag.AlbumArtists.Length > 0)
                                        {
                                            artist = media.Tag.AlbumArtists.First();
                                        }
                                    }
                                    album = media.Tag.Album;
                                    break;
                                }
                            }
                        }

                        client.UpdateDetails(title);
                        client.UpdateState(artist + " - " + album);
                        Console.WriteLine("Title: {0}\nArtist: {1}\nAlbum: {2}", title, artist, album);
                        client.Invoke();
                        System.Threading.Thread.Sleep(20000);
                    }
                }
                else
                {
                    
                    try
                    {
                        Console.Clear();
                        Console.WriteLine("GroovyRP\nhttps://github.com/dsdude123/GroovyRP\n\nGroove Music is not running or playing audio.");
                        client.ClearPresence();
                    }
                    catch (Exception e)
                    {
                        // This is okay, client may have not been initialized
                    }                  
                    System.Threading.Thread.Sleep(20000);
                }
            }

        }

        static bool audioCheck()
        {
            Process[] grooveMusics = Process.GetProcessesByName("Music.UI");
            if (grooveMusics.Length > 0)
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
                        bool targetProcess = false;
                        using (var sessionControl = session.QueryInterface<AudioSessionControl2>())
                        {
                            var process = sessionControl.Process;
                            if (process.ProcessName.Equals("Music.UI"))
                            {
                                targetProcess = true;
                            }
                        }
                        using (var audioMeter = session.QueryInterface<AudioMeterInformation>())
                        {
                            if (audioMeter.GetPeakValue() > 0 && targetProcess)
                            {
                                return true;
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

        public static readonly string[] supportedFileTypes = { "aa", "aax", "aac", "aiff", "ape", "dsf", "flac", "m4a", "m4b", "m4p", "mp3", "mpc", "mpp", "ogg", "oga", "wav", "wma", "wv", "webm" };
    }
}
