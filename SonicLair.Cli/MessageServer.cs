﻿using Newtonsoft.Json;

using SonicLair.Lib.Infrastructure;
using SonicLair.Lib.Services;
using SonicLair.Lib.Types;
using SonicLair.Lib.Types.SonicLair;

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

using WatsonWebsocket;

namespace SonicLair.Cli
{
    public class WebSocketService : INotificationObserver, INotifier
    {
        private readonly WatsonWsServer _server;
        private readonly ISubsonicService _subsonicService;
        private readonly IMusicPlayerService _player;
        private readonly bool _headless;
        public List<INotificationObserver> Observers { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public WebSocketService(ISubsonicService client, IMusicPlayerService player, bool headless)
        {
            _subsonicService = client;
            _player = player;
            _headless = headless;
            int PORT = 30002;
#pragma warning disable S2930 // "IDisposables" should be disposed
            UdpClient udpClient = new UdpClient
            {
                EnableBroadcast = true
            };
#pragma warning restore S2930 // "IDisposables" should be disposed
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
            var from = new IPEndPoint(0, 0);
            _server = new WatsonWsServer(StaticHelpers.GetLocalIp(), 30001);
            _server.ClientConnected += ClientConnected;
            _server.ClientDisconnected += ClientDisconnected;
            _server.MessageReceived += MessageReceived;
            _server.Start();
            _player.SetNotifier(this);
            _ = Task.Run(() =>
            {
                while (true)
                {
                    var recvBuffer = udpClient.Receive(ref from);
                    var s = Encoding.UTF8.GetString(recvBuffer);
                    if (s.Trim() == "soniclairClient")
                    {
                        var packet = Encoding.UTF8.GetBytes("soniclairServer");
                        udpClient.Send(packet, packet.Length, from.Address.ToString(), PORT);
                    }
                }
            });

        }

        private void Log(string message)
        {
            if (_headless)
            {
                Console.WriteLine(message);
            }
        }

        private void ClientConnected(object? sender, ClientConnectedEventArgs args)
        {
            Log($"Client connected from {args.IpPort}");
        }

        private void ClientDisconnected(object? sender, ClientDisconnectedEventArgs args)
        {
            Log($"{args.IpPort} disconnected");
        }

        private async Task Send(string ipport, string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _server.SendAsync(ipport, bytes);
        }

        private void MessageReceived(object? sender, MessageReceivedEventArgs args)
        {
            WebSocketMessage message;
            var s = Encoding.UTF8.GetString(args.Data);
            try
            {
                message = JsonConvert.DeserializeObject<WebSocketMessage>(s);
            }
            catch (Exception ex)
            {
                _ = Send(args.IpPort, ConstructMessage(ex.Message, "error"));
                return;
            }
            switch (message.Type)
            {
                case "jukebox":
                    var account = JsonConvert.DeserializeObject<Account>(message.Data);
                    if (_subsonicService.GetActiveAccount().Url != account.Url)
                    {
                        _ = Send(args.IpPort, ConstructMessage("You need to be logged in to the same server on both the phone and the TV for jukebox mode to work.", "error"));
                        _server.DisconnectClient(args.IpPort);
                        return;
                    }
                    _ = Send(args.IpPort, ConstructMessage("You're connected! Touch the TV icon in the upper left corner to disconnect."));
                    return;

                case "command":
                    var command = JsonConvert.DeserializeObject<WebSocketCommand>(message.Data);
                    switch (command.Command)
                    {
                        case "play":
                            _player.Play();
                            break;

                        case "pause":
                            _player.Pause();
                            break;

                        case "next":
                            _player.Next();
                            break;

                        case "prev":
                            _player.Prev();
                            break;

                        case "shufflePlaylist":
                            _player.Shuffle();
                            break;

                        case "playAlbum":
                            var albumParameters = command.Data.Split("|");
                            if (albumParameters.Length != 2)
                            {
                                _ = Send(args.IpPort, ConstructMessage("The parameters were malformed", "error"));
                                return;
                            }
                            _player.PlayAlbum(albumParameters[0], int.Parse(albumParameters[1]));
                            break;

                        case "playPlaylist":
                            var playlistParameters = command.Data.Split("|");
                            if (playlistParameters.Length != 2)
                            {
                                _ = Send(args.IpPort, ConstructMessage("The parameters were malformed", "error"));
                                return;
                            }
                            _player.PlayPlaylist(playlistParameters[0], int.Parse(playlistParameters[1]));
                            break;

                        case "playRadio":
                            _player.PlayRadio(command.Data);
                            break;

                        default:
                            break;
                    }
                    break;
            }
        }

        private string ConstructMessage(string text, string status = "ok")
        {
            var ret = new WebSocketMessage(text, "message", status);
            return JsonConvert.SerializeObject(ret);
        }

        public async void Update(string action, string value = null)
        {
            if (action.StartsWith("MS"))
            {
                var notif = new WebSocketNotification(action.Replace("MS", ""), value);
                var jsonNotif = JsonConvert.SerializeObject(notif, StaticHelpers.GetJsonSerializerSettings());
                var message = new WebSocketMessage(jsonNotif, "notification", "ok");
                var jsonMessage = JsonConvert.SerializeObject(message, StaticHelpers.GetJsonSerializerSettings());
                foreach (var s in _server.ListClients())
                {
                    Debug.WriteLine(jsonMessage);
                    await _server.SendAsync(s, jsonMessage);
                }
            }
        }

        public void NotifyObservers(string action, string value = null)
        {
            Update(action, value);
        }

        public void RegisterObserver(INotificationObserver observer)
        {
            throw new NotImplementedException();
        }

        public void UnregisterObserver(INotificationObserver observer)
        {
            throw new NotImplementedException();
        }
    }
}