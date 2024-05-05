using Sandbox.Services;

namespace Donut;

public sealed class PlayerController : Component
{
	public static PlayerController Local => NetworkManager.Players?.FirstOrDefault( x => x.SteamId == Game.SteamId );

	[Sync] public long SteamId { get; set; }
	[Sync] public string Time { get; set; }
	[Property, Sync] public GamePass DonatorPerk { get; set; }

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			SteamId = Game.SteamId;
		}
	}

	[Broadcast]
	public void SetName( string name )
	{
		GameObject.Name = name;
	}
}
