namespace Donut;

public sealed class Player : Component
{
	[Sync] public string Time { get; set; }

	public static Player Local => Game.ActiveScene.GetAllComponents<Player>().FirstOrDefault( ply => ply.Network.OwnerConnection == Connection.Local );
}
