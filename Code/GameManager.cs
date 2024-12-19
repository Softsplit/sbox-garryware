public enum GameState
{
	Intermission,
	Playing,
	Pause
}

public class Minigame
{
	public string Name { get; set; }
	public string Description { get; set; }
	public float Duration { get; set; }
	public Action OnStart { get; set; }
	public Action OnEnd { get; set; }
	public Func<bool> WinCondition { get; set; }
}

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Current { get; private set; }

	[Sync( Flags = SyncFlags.FromHost )] public GameState State { get; set; } = GameState.Intermission;
	[Sync( Flags = SyncFlags.FromHost )] public RealTimeSince TimeSinceStateStart { get; set; }
	[Sync( Flags = SyncFlags.FromHost )] public int MinPlayers { get; set; } = 2;

	[Sync( Flags = SyncFlags.FromHost )] private NetList<Minigame> Minigames { get; set; } = new();
	[Sync( Flags = SyncFlags.FromHost )] private int CurrentMinigameIndex { get; set; } = -1;

	public const float INTERMISSION_DURATION = 30f;
	public const float MINIGAME_DURATION = 5f;
	public const float PAUSE_DURATION = 5f;

	public Minigame CurrentMinigame => CurrentMinigameIndex >= 0 ? Minigames[CurrentMinigameIndex] : null;

	private ToastNotification Toast => ToastNotification.Current;

	private int lastTickSound = -1; // Track last second we played sound

	protected override void OnAwake()
	{
		Current = this;
	}

	protected override void OnEnabled()
	{
		if ( Networking.IsHost )
		{
			Networking.CreateLobby( new Sandbox.Network.LobbyConfig() );
			ChangeState( GameState.Intermission );
			RegisterMinigames();
		}
	}

	private void RegisterMinigames()
	{
		// Example: Jump minigame
		Minigames.Add( new Minigame
		{
			Name = "Jump!",
			Description = "Jump at least once to win!",
			Duration = MINIGAME_DURATION,
			OnStart = () => Toast?.AddToast( "Jump to win!" ),
			WinCondition = () => Scene.GetAllComponents<Player>()
				.Any( p => Input.Pressed( "jump" ) && !p.IsProxy )
		} );

		// Example: Don't Move
		Minigames.Add( new Minigame
		{
			Name = "Freeze!",
			Description = "Don't move!",
			Duration = MINIGAME_DURATION,
			OnStart = () => Toast?.AddToast( "Don't move!" ),
			WinCondition = () => Scene.GetAllComponents<Player>()
				.All( p => Input.AnalogMove.Length < 0.1f && !p.IsProxy )
		} );

		// Example: Look Up
		Minigames.Add( new Minigame
		{
			Name = "Look Up!",
			Description = "Look up at the sky!",
			Duration = MINIGAME_DURATION,
			OnStart = () => Toast?.AddToast( "Look up at the sky!" ),
			WinCondition = () => Scene.GetAllComponents<Player>()
				.Any( p => p.Controller.EyeTransform.Rotation.Pitch() < -45 && !p.IsProxy )
		} );
	}

	private void CheckGameState()
	{
		if ( !Networking.IsHost ) return;

		switch ( State )
		{
			case GameState.Intermission:
				if ( !HasMinimumPlayers() )
				{
					TimeSinceStateStart = 0; // Keep resetting timer until we have enough players
					return;
				}

				if ( TimeSinceStateStart >= INTERMISSION_DURATION )
				{
					StartNextMinigame();
				}
				break;

			case GameState.Playing:
				if ( !HasMinimumPlayers() )
				{
					Log.Info( "Not enough players, returning to intermission" );
					Toast?.AddToast( "Not enough players, returning to intermission" );
					ReturnToIntermission();
					return;
				}

				// Check if current minigame is done
				if ( TimeSinceStateStart >= MINIGAME_DURATION )
				{
					EvaluateAndStartPause();
				}
				break;

			case GameState.Pause:
				if ( !HasMinimumPlayers() )
				{
					ReturnToIntermission();
					return;
				}

				if ( TimeSinceStateStart >= PAUSE_DURATION )
				{
					StartNextMinigame();
				}
				break;
		}
	}

	private void StartNextMinigame()
	{
		if ( Minigames.Count == 0 ) return;

		// Select random minigame
		CurrentMinigameIndex = Random.Shared.Next( Minigames.Count );
		var minigame = CurrentMinigame;

		// Start the minigame
		ChangeState( GameState.Playing );
		RespawnAllPlayers();
		minigame.OnStart?.Invoke();
	}

	private void EvaluateAndStartPause()
	{
		var minigame = CurrentMinigame;
		if ( minigame == null ) return;

		// Check win condition
		bool succeeded = minigame.WinCondition?.Invoke() ?? false;

		// Play win/fail sound based on result
		Sound.Play( succeeded ? "win" : "fail" );

		Toast?.AddToast( succeeded ?
			$"You succeeded at {minigame.Name}!" :
			$"You failed {minigame.Name}!", 2.0f );

		ChangeState( GameState.Pause );
	}

	private void ReturnToIntermission()
	{
		CurrentMinigameIndex = -1;
		ChangeState( GameState.Intermission );
		RespawnAllPlayers();
	}

	protected override void OnDisabled()
	{
		if ( Current == this )
			Current = null;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		// Play ticktock sound every second during minigame
		if ( State == GameState.Playing )
		{
			int currentSecond = (int)TimeSinceStateStart;
			float timeLeft = MINIGAME_DURATION - TimeSinceStateStart;

			// Only play sound if we haven't played it this second
			// Changed to use >= 0 to include the final second
			if ( timeLeft >= 0 && currentSecond != lastTickSound )
			{
				Sound.Play( "ticktock" );
				lastTickSound = currentSecond;
			}
		}
		else
		{
			lastTickSound = -1; // Reset when not in minigame
		}

		CheckGameState();
	}

	void INetworkListener.OnActive( Connection channel )
	{
		SpawnPlayerForConnection( channel );
	}

	public bool HasMinimumPlayers() => Connection.All.Count >= MinPlayers;

	private void ChangeState( GameState newState )
	{
		State = newState;
		TimeSinceStateStart = 0;

		switch ( newState )
		{
			case GameState.Playing:
				Sound.Play( "start" );
				break;
		}
	}

	public void SpawnPlayerForConnection( Connection channel )
	{
		var startLocation = FindSpawnLocation().WithScale( 1 );

		var playerGo = GameObject.Clone( "/prefabs/player.prefab", new CloneConfig
		{
			Name = $"Player - {channel.DisplayName}",
			StartEnabled = true,
			Transform = startLocation
		} );

		var player = playerGo.Components.Get<Player>( true );
		playerGo.NetworkSpawn( channel );

		IPlayerEvent.PostToGameObject( player.GameObject, x => x.OnSpawned() );
	}

	[Rpc.Broadcast]
	private void RespawnAllPlayers()
	{
		var startLocation = FindSpawnLocation().WithScale( 1 );

		var player = Player.FindLocalPlayer();
		player.WorldTransform = startLocation;
		player.Network.ClearInterpolation();
	}

	/// <summary>
	/// Find the most appropriate place to respawn
	/// </summary>
	private Transform FindSpawnLocation()
	{
		//
		// If we have any SpawnPoint components in the scene, then use those
		//
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			return Random.Shared.FromArray( spawnPoints ).Transform.World;
		}

		//
		// Failing that, spawn where we are
		//
		return global::Transform.Zero;
	}

	[ConCmd( "gw_state" )]
	public static void CmdGameState()
	{
		if ( Current == null ) return;

		var timeLeft = Current.State == GameState.Intermission ?
			INTERMISSION_DURATION - Current.TimeSinceStateStart :
			MINIGAME_DURATION - Current.TimeSinceStateStart;

		Log.Info( $"Current State: {Current.State}, Time Left: {timeLeft:F1}s" );
		if ( Current.CurrentMinigame != null )
		{
			Log.Info( $"Current Minigame: {Current.CurrentMinigame.Name}" );
		}
	}

	[ConCmd( "gw_force_intermission" )]
	public static void CmdForceIntermission()
	{
		if ( !Networking.IsHost ) return;
		Current?.ReturnToIntermission();
	}

	[ConCmd( "gw_force_next" )]
	public static void CmdForceNextMinigame()
	{
		if ( !Networking.IsHost ) return;
		Current?.StartNextMinigame();
	}

	[ConCmd( "gw_set_minplayers" )]
	public static void CmdSetMinPlayers( int count )
	{
		if ( !Networking.IsHost ) return;
		if ( Current == null ) return;

		Current.MinPlayers = count;
		Log.Info( $"Minimum players set to {count}" );
		Current?.Toast?.AddToast( $"Minimum players set to {count}" );
	}
}
