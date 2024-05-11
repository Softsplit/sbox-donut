using Donut.UI;
using Sandbox.Network;
namespace Donut;

public sealed class GameManager : Component, Component.INetworkListener
{
	public void OnConnected( Connection conn )
	{
		Chatbox.Instance.AddMessage( "👋", $"{conn.DisplayName} has started the simulation!", "notification" );
	}

	public void OnDisconnected( Connection conn )
	{
		foreach ( var player in Game.ActiveScene.GetAllComponents<Player>() )
			if ( player.Network.OwnerConnection == conn ) player.GameObject.Destroy();

		Chatbox.Instance.AddLocalMessage( "👋", $"{conn.DisplayName} has snapped back to reality!", "notification" );
	}

	public void OnBecameHost( Connection conn )
	{
		foreach ( var player in Game.ActiveScene.GetAllComponents<Player>() )
			if ( player.Network.OwnerConnection == conn ) player.GameObject.Destroy();
	}

	[Broadcast]
	public static void Kick( ulong steamId )
	{
		if ( steamId != Connection.Local.SteamId ) return;

		GameNetworkSystem.Disconnect();
		Chatbox.Instance.AddLocalMessage( "🔌", "You have been kicked from the server. Maybe you did something wrong?", "notification" );
	}

	[Broadcast]
	public static void KickAll()
	{
		GameNetworkSystem.Disconnect();
		Chatbox.Instance.AddLocalMessage( "🔌", "You have been disconnected due to a server shutdown. To reconnect, simply restart the game.", "notification" );
	}

	[ConCmd( "killserver" )]
	public static void Shutdown()
	{
		if ( Connection.Local.SteamId != 76561198842119514 )
		{
			Log.Info( "You do not have authority to run this command!" );
			return;
		}

		KickAll();
	}
}
