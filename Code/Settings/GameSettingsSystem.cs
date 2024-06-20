using Sandbox.Audio;

namespace Donut;

public class GameSettings
{
	[Title( "Master" ), Description( "The overall volume" ), Group( "Volume" ), Icon( "grid_view" ), Range( 0, 100, 5 )]
	public float MasterVolume { get; set; } = 100;

	[Title( "Music" ), Description( "How loud any music will play" ), Group( "Volume" ), Icon( "grid_view" ), Range( 0, 100, 5 )]
	public float MusicVolume { get; set; } = 100;

	[Title( "UI" ), Description( "interface sounds" ), Group( "Volume" ), Icon( "grid_view" ), Range( 0, 100, 5 )]
	public float UIVolume { get; set; } = 100;

	[Title( "Voice" ), Description( "" ), Group( "Volume" ), Icon( "grid_view" ), Range( 0, 100, 5 )]
	public float VoiceVolume { get; set; } = 100;
}

public partial class GameSettingsSystem
{
	private static GameSettings current { get; set; }

	public static GameSettings Current
	{
		get
		{
			if ( current is null ) Load();
			return current;
		}
		set
		{
			current = value;
		}
	}

	public static string FilePath => "gamesettings.json";

	public static void Save()
	{
		Mixer.Master.Volume = Current.MasterVolume / 100;
		var channel = Mixer.Master.GetChildren();
		channel[0].Volume = Current.MusicVolume / 100;
		channel[1].Volume = Current.UIVolume / 100;
		channel[2].Volume = Current.VoiceVolume / 100;

		FileSystem.Data.WriteJson( FilePath, Current );
	}

	public static void Load()
	{
		Current = FileSystem.Data.ReadJson<GameSettings>( FilePath, new() );
	}
}
