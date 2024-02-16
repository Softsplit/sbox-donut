using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sandbox;
using Sandbox.Network;
using System.Linq;

public sealed class NetworkManager : Component, Component.INetworkListener
{
    public static NetworkManager Instance { get; private set; }

    public LobbyInformation Lobby { get; set; }

    public List<Connection> Connections = new();
    public Connection Host = null;
    [Sync] public long HostSteamId { get; set; }

    protected override void OnAwake()
    {
        base.OnAwake();

        Instance = this;
    }

    protected override async Task OnLoad()
    {
        if ( Scene.IsEditor )
            return;

        if ( !GameNetworkSystem.IsActive )
        {
            var lobbies = (await GameNetworkSystem.QueryLobbies())
                .Where( l => l.Members < l.MaxMembers && l.Name == "S&box Donut" )
                .OrderByDescending( l => l.Members );

            if ( lobbies.Count() > 0 )
            {
                Lobby = lobbies.First();
                GameNetworkSystem.Connect( Lobby.LobbyId );
            }
            else
            {
                GameNetworkSystem.CreateLobby();
            }
        }
    }


    /// <summary>
    /// A client is fully connected to the server. This is called on the host.
    /// </summary>
    public void OnActive( Connection channel )
    {
        Log.Info( $"Player '{channel.DisplayName}' has joined the game" );

        Connections.Add( channel );

        if ( Connections.Count == 1 )
        {
            Host = channel;
            HostSteamId = (long)channel.SteamId;
        }
    }

    public void OnDisconnected( Connection channel )
    {
        Connections.Remove( channel );
    }

    public void OnBecameHost( Connection previousHost )
    {
        Host = Connections.FirstOrDefault( x => x.SteamId == (ulong)Game.SteamId );
        HostSteamId = (long)Game.SteamId;

        Log.Info( "You are now the host!" );
    }
}