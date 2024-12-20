public enum GameState
{
	Intermission,
	Playing,
	Pause
}

public sealed class GameManager : Component, Component.INetworkListener
{
	public static GameManager Current { get; private set; }

	[Property, Sync( Flags = SyncFlags.FromHost )] public GameState State { get; private set; } = GameState.Intermission;
	[Sync( Flags = SyncFlags.FromHost )] public RealTimeSince TimeInState { get; private set; }
	[Sync( Flags = SyncFlags.FromHost )] public int MinPlayers { get; set; } = 2;
	[Sync( Flags = SyncFlags.FromHost )] public int CurrentMinigameIndex { get; set; } = -1;

	private List<Minigame> Minigames { get; set; } = new();

	public const float INTERMISSION_DURATION = 10f;
	public const float MINIGAME_DURATION = 5f;
	public const float PAUSE_DURATION = 5f;

	public Minigame CurrentMinigame => CurrentMinigameIndex >= 0 ? Minigames[CurrentMinigameIndex] : null;

	public float TimeLeft => State switch
	{
		GameState.Intermission => INTERMISSION_DURATION - TimeInState,
		GameState.Playing => MINIGAME_DURATION - TimeInState,
		GameState.Pause => PAUSE_DURATION - TimeInState,
		_ => 0
	};

	private ToastNotification Toast => ToastNotification.Current;

	protected override void OnAwake()
	{
		Current = this;
	}

	protected override void OnEnabled()
	{
		RegisterMinigames();

		if ( Networking.IsHost )
		{
			Networking.CreateLobby( new Sandbox.Network.LobbyConfig() );
			ChangeState( GameState.Intermission );
		}
	}

	private void RegisterMinigames()
	{
		foreach ( var type in TypeLibrary.GetTypes<Minigame>() )
		{
			if ( type.Name == "Minigame" )
				continue;

			var minigameInstance = type.Create<Minigame>();

			if ( minigameInstance == null )
				continue;

			Minigames.Add( minigameInstance );
		}
	}

	private void ChangeState( GameState newState )
	{
		State = newState;
		TimeInState = 0;

		switch ( State )
		{
			case GameState.Intermission:
				CurrentMinigameIndex = -1;
				break;
			case GameState.Playing:
				StartMinigame();
				break;
		}
	}

	private void StartMinigame()
	{
		if ( Minigames.Count == 0 ) return;

		CurrentMinigameIndex = Random.Shared.Next( Minigames.Count );
		var minigame = CurrentMinigame;

		RespawnAllPlayers();
		minigame.OnStart();
	}

	int lastTickSound = -1;

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		HandleStateLogic();

		if ( State == GameState.Playing )
		{
			int currentSecond = (int)TimeInState;
			float timeLeft = MINIGAME_DURATION - TimeInState;

			if ( timeLeft >= 0 && currentSecond != lastTickSound )
			{
				PlaySound( "ticktock" );
				lastTickSound = currentSecond;
			}
		}
		else
		{
			lastTickSound = -1;
		}
	}

	private void HandleStateLogic()
	{
		if ( !HasMinimumPlayers() )
		{
			if ( State != GameState.Intermission )
			{
				Log.Info( "Not enough players, returning to intermission" );
				DisplayToast( "Not enough players, returning to intermission" );
				ChangeState( GameState.Intermission );
			}

			TimeInState = 0;
			return;
		}

		switch ( State )
		{
			case GameState.Intermission:
				if ( TimeInState >= INTERMISSION_DURATION )
				{
					PlaySound( "start" );
					ChangeState( GameState.Playing );
				}
				break;
			case GameState.Playing:
				if ( TimeInState >= CurrentMinigame.Duration )
				{
					EvaluateMinigame();
					ChangeState( GameState.Pause );
				}
				CurrentMinigame.OnFixedUpdate();
				break;
			case GameState.Pause:
				if ( TimeInState >= PAUSE_DURATION )
				{
					PlaySound( "start" );
					ChangeState( GameState.Playing );
				}
				break;
		}
	}

	private void EvaluateMinigame()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			var minigame = CurrentMinigame;
			if ( minigame == null ) return;

			bool succeeded = minigame.WinCondition( player );

			PlaySound( succeeded ? "win" : "fail", player );

			DisplayToast( succeeded ?
				$"You succeeded!" :
				$"You failed!", 2.0f,
				player );
		}
	}

	public bool HasMinimumPlayers() => Connection.All.Count >= MinPlayers;

	void INetworkListener.OnActive( Connection channel )
	{
		SpawnPlayerForConnection( channel );
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

	private Transform FindSpawnLocation()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			return Random.Shared.FromArray( spawnPoints ).Transform.World;
		}

		return global::Transform.Zero;
	}

	[Rpc.Broadcast]
	public void DisplayToast( string text, float duration = 3.0f, Player to = null )
	{
		if ( to != null && Connection.Local != to.Network.Owner )
			return;

		Toast?.AddToast( text, duration );
	}

	[Rpc.Broadcast]
	public void PlaySound( string sound, Player to = null )
	{
		if ( to != null && Connection.Local != to.Network.Owner )
			return;

		Sound.Play( sound );
	}

	[ConCmd( "gw_state" )]
	public static void CmdGameState()
	{
		if ( Current == null ) return;

		Log.Info( $"Current State: {Current.State}, Time Left: {Current.TimeLeft:F1}s" );
		if ( Current.CurrentMinigame != null )
		{
			Log.Info( $"Current Minigame: {Current.CurrentMinigame.Name}" );
		}
	}

	[ConCmd( "gw_force_intermission" )]
	public static void CmdForceIntermission()
	{
		if ( !Networking.IsHost ) return;
		Current?.ChangeState( GameState.Intermission );
	}

	[ConCmd( "gw_force_next" )]
	public static void CmdForceNextMinigame()
	{
		if ( !Networking.IsHost ) return;
		Current?.ChangeState( GameState.Playing );
	}

	[ConCmd( "gw_set_minplayers" )]
	public static void CmdSetMinPlayers( int count )
	{
		if ( !Networking.IsHost ) return;
		if ( Current == null ) return;

		Current.MinPlayers = count;
		Log.Info( $"Minimum players set to {count}" );
		Current.Toast?.AddToast( $"Minimum players set to {count}" );
	}
}
