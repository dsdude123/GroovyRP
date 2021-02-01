using System;
using System.Diagnostics;
using DiscordRPC;
using DiscordRPC.Message;

namespace GroovyRP
{
    class Program
    {
        private const string appDetails = "GroovyRP\nhttps://github.com/dsdude123/GroovyRP\n";
        private static readonly DiscordRpcClient _client = new DiscordRpcClient("737646410354130955", autoEvents: false);
        private static readonly GrooveInfoFetcher _grooveInfoFetcher = new GrooveInfoFetcher();
        private static string pressenceDetails = string.Empty;

        private static void Main()
        {
            Console.WriteLine(appDetails);
            Console.WriteLine("\nNothing Playing");

            _client.Initialize();
            _client.OnError += _client_OnError;
            _client.OnPresenceUpdate += _client_OnPresenceUpdate;

            TrackInfo currentTrack = new TrackInfo();
            TrackInfo oldTrack = new TrackInfo();

            bool isPresenceActive = false;
            bool hasGrooveMusicStartedOnce = false;

            while (_client.IsInitialized)
            {
                Process[] grooveMusics = Process.GetProcessesByName("Music.UI");
                if (hasGrooveMusicStartedOnce && grooveMusics.Length < 1)
                {
                    Environment.Exit(0);
                } else if (grooveMusics.Length > 0)
                {
                    hasGrooveMusicStartedOnce = true;
                }
                if (_grooveInfoFetcher.IsUsingAudio())
                {
                    try
                    {
                        currentTrack = _grooveInfoFetcher.GetTrackInfo();
                        if (oldTrack.Title != currentTrack.Title)
                        {
                            var details = $"Title: {currentTrack.Title}";
                            var state = $"Artist: {currentTrack.Artist}";

                            _client.SetPresence(new RichPresence
                            {
                                Details = details,
                                State = state,
                                Assets = new Assets
                                {
                                    LargeImageKey = "groove",
                                    LargeImageText = "Groove Music",
                                    SmallImageKey = "groove_small"
                                }
                            });
                            isPresenceActive = true;
                            _client.Invoke();
                        }
                    }
                    catch (Exception)
                    {
                        isPresenceActive = true;
                        _client.SetPresence(new RichPresence()
                        {
                            Details = "Failed to get track info"
                        });
                        Console.Clear();
                        Console.WriteLine(appDetails);
                        Console.WriteLine("\nFailed to get track info");
                    }
                }
                else
                {
                    _client.ClearPresence();
                    oldTrack = new TrackInfo();
                    if(isPresenceActive)
                    {
                        Console.Clear();
                        Console.WriteLine(appDetails);
                        Console.WriteLine("\nNothing Playing");
                        isPresenceActive = false;
                    }
                }
            }
        }

        private static void _client_OnPresenceUpdate(object sender, PresenceMessage args)
        {
            if (args.Presence != null)
            {
                if (pressenceDetails != args.Presence.Details)
                {
                    pressenceDetails = _client.CurrentPresence?.Details;
                    Console.Clear();
                    Console.WriteLine(appDetails);
                    Console.WriteLine($"{args.Presence.Details}, {args.Presence.State}");
                }
            }
            else
            {
                Console.Clear();
                Console.WriteLine(appDetails);
                Console.WriteLine("\nNothing Playing");
                pressenceDetails = string.Empty;
            }
        }

        private static void _client_OnError(object sender, ErrorMessage args)
        {
            Console.WriteLine(args.Message);
        }
    }
}
