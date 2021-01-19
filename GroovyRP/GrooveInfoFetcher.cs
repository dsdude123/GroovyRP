using CSCore.CoreAudioAPI;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace GroovyRP
{
    public class GrooveInfoFetcher
    {
        public TrackInfo GetTrackInfo()
        {
            var result = new TrackInfo();

            var handleFinder = Process.Start("openedfilesview\\OpenedFilesView.exe", "/processfilter Music.UI.exe /scomma files.csv");
            handleFinder.WaitForExit();
            var streamReader = File.OpenText("files.csv");
            var csvParser = new CsvParser(streamReader, CultureInfo.InvariantCulture);

            var row = csvParser.Read();
            var rows = new List<string[]>();

            while (row != null)
            {
                rows.Add(row);
                row = csvParser.Read();
            }

            foreach (string[] data in rows)
            {
                try
                {
                    if (supportedFileTypes.Contains(data[20], StringComparer.OrdinalIgnoreCase))
                    {
                        var media = TagLib.File.Create(data[1]);

                        result.Title = media.Tag.Title;
                        result.Artist = media.Tag.AlbumArtists.First() ?? "Unknown Artist";
                        result.Album = media.Tag.Album ?? "Unknown Album";

                        break;
                    }
                    streamReader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message} \n{ex.StackTrace}");
                    return result;
                }
            }
            csvParser.Dispose();
            streamReader.Dispose();
            return result;
        }

        public bool IsUsingAudio()
        {
            var grooveMusics = Process.GetProcessesByName("Music.UI");
            if (grooveMusics.Any())
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

        private static readonly string[] supportedFileTypes = {
            "aa",
            "aax",
            "aac",
            "aiff",
            "ape",
            "dsf",
            "flac",
            "m4a",
            "m4b",
            "m4p",
            "mp3",
            "mpc",
            "mpp",
            "ogg",
            "oga",
            "wav",
            "wma",
            "wv",
            "webm"
        };
    }
}
