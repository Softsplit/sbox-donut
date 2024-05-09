using Sandbox.Services;
namespace Donut;

public sealed class Player : Component
{
	[Property] public GamePass DonatorPerk { get; set; }

	[Sync] public string Time { get; set; }

	public static Player Local => Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault( player => player.Network.OwnerConnection == Connection.Local );
}
