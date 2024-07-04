using Donut.UI;
using Sandbox.Audio;
using Sandbox.Network;
using Sandbox.Services;
using System;

namespace Donut;

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Instance { get; private set; }

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

	// this is hardcoded, but it works so who cares
	public static List<string> Songs => new() {
		"https://cdn.sbox.game/asset/sounds/music/aquarium.mp3.efd8e8fd5261ec86",
		"https://cdn.sbox.game/asset/sounds/music/addiction.mp3.c749cc006ba3d2b4",
		"https://cdn.sbox.game/asset/sounds/music/eek.mp3.a0ac0192b999f9ab",
		"https://cdn.sbox.game/asset/sounds/music/dead_lock.mp3.a3aee6aba5e27b9f",
		"https://cdn.sbox.game/asset/sounds/music/da_jungle_is_wicked.mp3.845a3efa764820ca",
		"https://cdn.sbox.game/asset/sounds/music/empty.mp3.b900a9b179afdc18",
		"https://cdn.sbox.game/asset/sounds/music/elysium.mp3.9575b0c58bed46d6",
		"https://cdn.sbox.game/asset/sounds/music/celestial_fantasia.mp3.384b309b00f42a02",
		"https://cdn.sbox.game/asset/sounds/music/funky_stars.mp3.35b64c6021fc43da",
		"https://cdn.sbox.game/asset/sounds/music/foregone_destruction.mp3.584685f34a9026c7",
		"https://cdn.sbox.game/asset/sounds/music/aryx.mp3.fc6157a873f29626",
		"https://cdn.sbox.game/asset/sounds/music/guitar_slinger.mp3.8ba13fc39b2ac60e",
		"https://cdn.sbox.game/asset/sounds/music/keygen_8.mp3.f953266131ebb0a3",
		"https://cdn.sbox.game/asset/sounds/music/ledstorm.mp3.bc681bf3969fa6f3",
		"https://cdn.sbox.game/asset/sounds/music/nine_one_one.mp3.f5508182d66d46a4",
		"https://cdn.sbox.game/asset/sounds/music/point_of_departure.mp3.ab956b2158316732",
		"https://cdn.sbox.game/asset/sounds/music/hyperbased.mp3.9273149c23d93eff",
		"https://cdn.sbox.game/asset/sounds/music/jungle_love.mp3.dacfd0c1e2d2adc7",
		"https://cdn.sbox.game/asset/sounds/music/in_my_life_my_mind.mp3.365e1375cfbc8da8",
		"https://cdn.sbox.game/asset/sounds/music/rolling_down_the_street.mp3.2716ecdf3a31dbf2",
		"https://cdn.sbox.game/asset/sounds/music/space_debris.mp3.c40d68dedc3a4198",
		"https://cdn.sbox.game/asset/sounds/music/stardust_memories.mp3.d845fc9f324afd9a",
		"https://cdn.sbox.game/asset/sounds/music/unreal_superhero_3.mp3.6ac5cee6d904a666",
		"https://cdn.sbox.game/asset/sounds/music/unreal_2.mp3.e81399ae13431a1f",
		"https://cdn.sbox.game/asset/sounds/music/the_great_strategy.mp3.493e41d987c5de02",
		"https://cdn.sbox.game/asset/sounds/music/winds_of_fjords.mp3.b3a64344d9c0a3d3",
		"https://cdn.sbox.game/asset/sounds/music/yuki_satellites.mp3.ec2036f5bb16c60f"
	};

	public MusicPlayer MusicPlayer { get; private set; }

	private void UpdateMusic()
	{
		if ( MusicPlayer == null )
		{
			MusicPlayer = MusicPlayer.PlayUrl( Songs.OrderBy( s => Guid.NewGuid() ).First() );
			MusicPlayer.TargetMixer = Mixer.FindMixerByName( "Music" );
			MusicPlayer.TargetMixer.DistanceAttenuation = 0f;
			MusicPlayer.TargetMixer.Spacializing = 0f;
		}

		MusicPlayer.OnFinished = () => { MusicPlayer = null; };
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
