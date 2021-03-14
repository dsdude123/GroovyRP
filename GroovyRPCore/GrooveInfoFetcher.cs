using CSCore.CoreAudioAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Windows.Media.Control;

namespace GroovyRP
{
    public class GrooveInfoFetcher
    {
        public TrackInfo GetTrackInfo()
        {
            var result = new TrackInfo();

            GlobalSystemMediaTransportControlsSessionMediaProperties currentTrack = null;
            currentTrack = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult().GetCurrentSession().TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
            result.Title = currentTrack.Title ?? "Unknown Title";
            result.Artist = currentTrack.Artist ?? currentTrack.AlbumArtist ?? "Unknown Artist";
            result.Album = currentTrack.AlbumTitle ?? currentTrack.AlbumTitle;
     
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
                            if (process != null && process.ProcessName.Equals("Music.UI"))
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
