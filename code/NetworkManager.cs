using System.Collections.Generic;
using Sandbox;
using System.Linq;
using Sandbox.Network;
using System.Threading.Tasks;
namespace Donut;

public sealed class NetworkManager : Component, Component.INetworkListener
{
    public static NetworkManager Instance { get; private set; }

    /// <summary>
	/// Create a server (if we're not joining one)
	/// </summary>
	[Property] public bool StartServer { get; set; } = true;

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

    public List<Connection> Connections = new();
    public Connection Host = null;
    [Sync] public long HostSteamId { get; set; }

    public List<PlayerController> Players => Game.ActiveScene.Components.GetAll<PlayerController>(FindMode.EnabledInSelfAndDescendants).ToList();

    protected override void OnAwake()
    {
        base.OnAwake();

        Instance = this;
    }

    protected override async Task OnLoad()
	{
		if (Scene.IsEditor)
			return;

		if (StartServer && !GameNetworkSystem.IsActive)
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds(0.1f);
			GameNetworkSystem.CreateLobby();
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

        if (PlayerPrefab is null)
			return;

        SpawnPlayer(channel);
    }

	public void SpawnPlayer( Connection channel )
	{
		var startLocation = Transform.World;

		startLocation.Scale = 1;

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( startLocation, name: $"Player - {channel.DisplayName}" );
		player.NetworkSpawn( channel );

		var playerController = player.Components.Get<PlayerController>( FindMode.EverythingInSelfAndDescendants );
		playerController.SetName( channel.DisplayName );
	}

	public void OnDisconnected( Connection channel )
    {
        foreach (var player in Players)
		{
			if (player.SteamId == (long)channel.SteamId)
			{
				player.GameObject.Destroy();
			}
		}

        Connections.Remove( channel );
    }

    public void OnBecameHost( Connection previousHost )
    {
        foreach (var player in Players)
		{
			if (player.SteamId == (long)previousHost.SteamId)
			{
				player.GameObject.Destroy();
			}
		}

        Host = Connections.FirstOrDefault( x => x.SteamId == (ulong)Game.SteamId );
        HostSteamId = Game.SteamId;

        Log.Info( "You are now the host!" );
    }
}