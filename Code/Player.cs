using Sandbox.Services;
namespace Donut;

public sealed class Player : Component
{
	[Property] public GamePass DonatorPerk { get; set; }

	[Sync] public long SteamId { get; set; }
	[Sync] public string Time { get; set; }

	public static Player Local => NetworkManager.Players?.FirstOrDefault( x => x.SteamId == Game.SteamId );

	protected override void OnUpdate()
	{
		if ( !IsProxy )
			SteamId = Game.SteamId;
	}

	[Broadcast]
	public void SetName( string name )
	{
		GameObject.Name = name;
	}
}
