using Donut.UI;
using Sandbox.Audio;
using Sandbox.Network;
using Sandbox.Services;
using System;

namespace Donut;

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Instance { get; private set; }
	public static List<string> Songs { get; private set; } = FileSystem.Mounted.FindFile( "sounds/music", "*.mp3" ).ToList();

	public MusicPlayer MusicPlayer { get; private set; }
	public Leaderboards.Board Leaderboard { get; private set; }

	public bool LeaderboardSwitching { get; private set; } = false;

	protected override async void OnAwake()
	{
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

	private void UpdateMusic()
	{
		if ( MusicPlayer == null )
		{
			MusicPlayer = MusicPlayer.Play( FileSystem.Mounted, $"sounds/music/{Songs.OrderBy( s => Guid.NewGuid() ).First()}" );

			MusicPlayer.TargetMixer = Mixer.FindMixerByName( "Music" );
			MusicPlayer.TargetMixer.DistanceAttenuation = 0f;
			MusicPlayer.TargetMixer.Spacializing = 0f;
		}

		MusicPlayer.OnFinished = () =>
		{
			MusicPlayer = null;
		};
	}

	private void UpdateInput()
	{
		if ( DonutCanvas.Instance == null )
			return;

		if ( Input.Pressed( "Adjust Rotation Speed (+)" ) )
		{
			DonutCanvas.Instance.DELTA_A += 0.0025;
			DonutCanvas.Instance.DELTA_B += 0.0025;
			Sound.Play( "increase" ).TargetMixer = Mixer.FindMixerByName( "UI" );
		}

		if ( Input.Pressed( "Adjust Rotation Speed (-)" ) )
		{
			DonutCanvas.Instance.DELTA_A -= 0.0025;
			DonutCanvas.Instance.DELTA_B -= 0.0025;
			Sound.Play( "decrease" ).TargetMixer = Mixer.FindMixerByName( "UI" );
		}

		if ( Input.Pressed( "Reset Rotation Speed" ) )
		{
			DonutCanvas.Instance.DELTA_A = 0.04;
			DonutCanvas.Instance.DELTA_B = 0.02;
			Sound.Play( "reset" ).TargetMixer = Mixer.FindMixerByName( "UI" );
		}

		if ( Input.Pressed( "Munch" ) )
		{
			Munch();
		}
	}

	public void Munch()
	{
		if ( DonutCanvas.Instance.Clicks < 7 )
		{
			DonutCanvas.Instance.Clicks += 1;
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

	private void UpdateDonut()
	{
		if ( DonutCanvas.Instance == null )
			return;

		if ( DonutCanvas.Instance.Clicks >= 7 )
		{
			DonutCanvas.Instance.Clicks = 0;
			Stats.Increment( "donuts", 1 );
			Sound.Play( "splat" ).TargetMixer = Mixer.FindMixerByName( "UI" );
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
