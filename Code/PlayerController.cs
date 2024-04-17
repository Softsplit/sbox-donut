namespace Donut;

public sealed class PlayerController : Component
{
	public static PlayerController Local => NetworkManager.Instance.Players.FirstOrDefault( x => x.SteamId == Game.SteamId );

	[Sync] public long SteamId { get; set; }
	[Sync] public string Time { get; set; }

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
