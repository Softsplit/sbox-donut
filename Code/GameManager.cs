using Donut.UI;
using Sandbox.Audio;
using Sandbox.Services;

namespace Donut;

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Instance { get; private set; }

	public Leaderboards.Board Leaderboard { get; private set; }

	public bool LeaderboardSwitching { get; private set; } = false;

	protected override async void OnAwake()
	{
		Instance = this;

		PlaySong();

		LeaderboardSwitching = true;

		Leaderboard = Leaderboards.Get( "newtime" );
		Leaderboard.MaxEntries = 100;

		await Leaderboard?.Refresh();

		LeaderboardSwitching = false;
	}

	protected override void OnStart()
	{
		Chatbox.Instance?.AddLocalMessage(
			"👋",
			"Welcome to S&box Donut! " +
			"Press the right and left arrow keys to adjust the rotation speed and R to reset. " +
			"You can press V to use the voice chat or ENTER to open up the chatbox. " +
			"Finally, click on the donut or press SPACEBAR to munch on it, have fun!",
			"notification"
		);
	}

	protected override void OnUpdate()
	{
		UpdateInput();
		UpdateLeaderboard();
		UpdateTime();
		UpdateDonut();
	}

	public MusicPlayer MusicPlayer { get; private set; }

	public static string[] Songs => FileSystem.Mounted.FindFile( "sounds/music", "*.mp3" ).ToArray();

	private int currentSongIndex;

	private void PlaySong()
	{
		MusicPlayer?.Stop();
		MusicPlayer?.Dispose();

		if ( currentSongIndex >= Songs.Length )
			currentSongIndex = 0;

		string songToPlay = $"sounds/music/{Songs[currentSongIndex]}";
		currentSongIndex++;

		MusicPlayer = MusicPlayer.Play( FileSystem.Mounted, songToPlay );
		MusicPlayer.TargetMixer = Mixer.FindMixerByName( "Music" );
		MusicPlayer.OnFinished += PlaySong;
	}

	private void UpdateInput()
	{
		if ( !Canvas.Instance.IsValid() )
			return;

		if ( Input.Pressed( "Adjust Rotation Speed (+)" ) )
		{
			Canvas.Instance.DELTA_A += 0.0025;
			Canvas.Instance.DELTA_B += 0.0025;
			Sound.Play( "increase" ).TargetMixer = Mixer.FindMixerByName( "UI" );
		}

		if ( Input.Pressed( "Adjust Rotation Speed (-)" ) )
		{
			Canvas.Instance.DELTA_A -= 0.0025;
			Canvas.Instance.DELTA_B -= 0.0025;
			Sound.Play( "decrease" ).TargetMixer = Mixer.FindMixerByName( "UI" );
		}

		if ( Input.Pressed( "Reset Rotation Speed" ) )
		{
			Canvas.Instance.DELTA_A = 0.04;
			Canvas.Instance.DELTA_B = 0.02;
			Sound.Play( "reset" ).TargetMixer = Mixer.FindMixerByName( "UI" );
		}

		if ( Input.Pressed( "Munch" ) )
		{
			Munch();
		}
	}

	public double Clicks { get; private set; } = 0;

	private void UpdateDonut()
	{
		if ( Clicks >= 7 )
		{
			Clicks = 0;
			Stats.Increment( "donuts", 1 );
			Sound.Play( "splat" ).TargetMixer = Mixer.FindMixerByName( "UI" );
		}
	}

	public void Munch()
	{
		if ( Clicks < 7 )
		{
			Clicks += 1;
			Sound.Play( "munch" ).TargetMixer = Mixer.FindMixerByName( "UI" );
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

	public void OnConnected( Connection conn )
	{
		Chatbox.Instance?.AddMessage( "👋", $"{conn.DisplayName} has started the simulation!", "notification" );
	}

	public void OnDisconnected( Connection conn )
	{
		foreach ( var ply in Game.ActiveScene.GetAllComponents<Player>() )
			if ( ply.Network.Owner == conn ) ply.GameObject.Destroy();

		Chatbox.Instance?.AddMessage( "👋", $"{conn.DisplayName} has snapped back to reality!", "notification" );
	}

	public static void Kick( Connection conn )
	{
		Chatbox.Instance?.AddMessage( "🔌", $"{conn.DisplayName} has been kicked from the server. Good riddance!", "notification" );
		conn.Kick( "You have been kicked from the server. Sorry, not sorry!" );
	}

	[ConCmd( "killserver" )]
	public static void Shutdown()
	{
		if ( Connection.Local.SteamId != 76561198842119514 )
		{
			Log.Info( "You do not have authority to run this command!" );
			return;
		}

		foreach ( var conn in Connection.All )
		{
			Kick( conn );
		}
	}
}
