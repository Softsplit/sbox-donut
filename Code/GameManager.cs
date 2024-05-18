using Donut.UI;
using Sandbox.Audio;
using Sandbox.Network;
using Sandbox.Services;

namespace Donut;

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Instance { get; private set; }
	public static string[] Songs { get; private set; } = FileSystem.Mounted.FindFile( "sounds/music", "*.mp3" ).ToArray();

	public MusicPlayer MusicPlayer { get; private set; }
	public Leaderboards.Board Leaderboard { get; private set; }

	public bool LeaderboardSwitching { get; private set; } = false;

	protected override async void OnAwake()
	{
		var gameMixer = Mixer.FindMixerByName( "Game" );
		gameMixer.Occlusion = 0f;
		gameMixer.Spacializing = 0f;
		gameMixer.AirAbsorption = 0f;
		gameMixer.DistanceAttenuation = 0f;

		LeaderboardSwitching = true;

		Leaderboard = Leaderboards.Get( "newtime" );
		Leaderboard.MaxEntries = 100;

		await Leaderboard?.Refresh();

		LeaderboardSwitching = false;
	}

	protected override void OnUpdate()
	{
		Instance = this;

		UpdateMusic();
		UpdateInput();
		UpdateLeaderboard();
		UpdateTime();
		UpdateDonut();
	}

	private readonly List<int> songIndices = new();

	private void UpdateMusic()
	{
		if ( MusicPlayer == null )
		{
			int newIndex;

			do { newIndex = Game.Random.Next( 0, Songs.Length ); } while ( songIndices.Contains( newIndex ) );

			songIndices.Add( newIndex );

			if ( songIndices.Count == Songs.Length )
				songIndices.Clear();

			MusicPlayer = MusicPlayer.Play( FileSystem.Mounted, $"sounds/music/{Songs[newIndex]}" );
			MusicPlayer.TargetMixer = Mixer.FindMixerByName( "Music" );
		}

		MusicPlayer.OnFinished = () => { MusicPlayer = null; };
	}

	private void UpdateInput()
	{
		if ( UI.Donut.Instance == null )
			return;

		if ( Input.Pressed( "Increase Rotation Speed" ) )
		{
			UI.Donut.Instance.DELTA_A += 0.0025;
			UI.Donut.Instance.DELTA_B += 0.0025;
			Sound.PlayFile( SoundFile.Load( "sounds/increase.wav" ) );
		}

		if ( Input.Pressed( "Decrease Rotation Speed" ) )
		{
			UI.Donut.Instance.DELTA_A -= 0.0025;
			UI.Donut.Instance.DELTA_B -= 0.0025;
			Sound.PlayFile( SoundFile.Load( "sounds/decrease.wav" ) );
		}

		if ( Input.Pressed( "Reset Rotation Speed" ) )
		{
			UI.Donut.Instance.DELTA_A = 0.04;
			UI.Donut.Instance.DELTA_B = 0.02;
			Sound.PlayFile( SoundFile.Load( "sounds/reset.wav" ) );
		}
	}

	public void Munch()
	{
		if ( UI.Donut.Instance.yay < 7 )
		{
			UI.Donut.Instance.yay += 1;
			Sound.PlayFile( SoundFile.Load( "sounds/munch.wav" ) );
		}
	}

	private readonly string[] leaderboards = new[] { "newtime", "donuts" };
	private int currentLeaderboard;

	public async void SwitchLeaderboard()
	{
		LeaderboardSwitching = true;

		currentLeaderboard = (currentLeaderboard + 1) % leaderboards.Length;
		Leaderboard = Leaderboards.Get( leaderboards[currentLeaderboard] );
		Leaderboard.MaxEntries = 100;

		await Leaderboard?.Refresh();

		LeaderboardSwitching = false;
	}

	private RealTimeUntil leaderboardDelay;

	private void UpdateLeaderboard()
	{
		if ( leaderboardDelay )
		{
			leaderboardDelay = 30f;

			Leaderboard?.Refresh();
		}
	}

	private int hours;
	private int minutes;
	private int seconds;
	private RealTimeUntil timeDelay;

	private void UpdateTime()
	{
		if ( timeDelay )
		{
			timeDelay = 1f;

			seconds++;
			if ( seconds == 60 )
			{
				seconds = 0;
				minutes++;
			}
			if ( minutes == 60 )
			{
				minutes = 0;
				hours++;
			}

			if ( Player.Local.IsValid() )
				Player.Local.Time = $"{hours}h{minutes}m{seconds}s";

			Stats.Increment( "time2", 1f );
		}
	}

	private void UpdateDonut()
	{
		if ( UI.Donut.Instance == null )
			return;

		if ( UI.Donut.Instance.yay >= 7 )
		{
			UI.Donut.Instance.yay = 0;
			Stats.Increment( "donuts", 1 );
			Sound.PlayFile( SoundFile.Load( "sounds/splat.ogg" ) );
		}
	}

	public void OnConnected( Connection conn )
	{
		Chatbox.Instance?.AddMessage( "👋", $"{conn.DisplayName} has started the simulation!", "notification" );
	}

	public void OnDisconnected( Connection conn )
	{
		foreach ( var ply in Game.ActiveScene.GetAllComponents<Player>() )
			if ( ply.Network.OwnerConnection == conn ) ply.GameObject.Destroy();

		Chatbox.Instance?.AddLocalMessage( "👋", $"{conn.DisplayName} has snapped back to reality!", "notification" );
	}

	public void OnBecameHost( Connection conn )
	{
		foreach ( var ply in Game.ActiveScene.GetAllComponents<Player>() )
			if ( ply.Network.OwnerConnection == conn ) ply.GameObject.Destroy();
	}

	[Broadcast]
	public static void Kick( ulong steamId )
	{
		if ( steamId != Connection.Local.SteamId ) return;

		GameNetworkSystem.Disconnect();
		Chatbox.Instance?.AddLocalMessage( "🔌", "You have been kicked from the server. Maybe you did something wrong?", "notification" );
	}

	[Broadcast]
	public static void KickAll()
	{
		GameNetworkSystem.Disconnect();
		Chatbox.Instance?.AddLocalMessage( "🔌", "You have been disconnected due to a server shutdown. To reconnect, simply restart the game.", "notification" );
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
