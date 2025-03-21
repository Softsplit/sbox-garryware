public enum GameState
{
	Intermission,
	Playing,
	Pause
}

public sealed partial class GameManager : Component, Component.INetworkListener
{
	public static GameManager Current { get; private set; }

	[Property] public List<GameObject> SpawnGroups { get; set; }

	[Property, Sync( Flags = SyncFlags.FromHost )]
	public GameState State { get; private set; } = GameState.Intermission;

	[Sync( Flags = SyncFlags.FromHost )] public RealTimeSince TimeInState { get; private set; }
	[Sync( Flags = SyncFlags.FromHost )] public int MinPlayers { get; set; } = 2;
	[Sync( Flags = SyncFlags.FromHost )] public int CurrentMinigameIndex { get; set; } = -1;

	private List<Minigame> Minigames { get; set; } = new();

	public const float INTERMISSION_DURATION = 15f;

	public const float MINIGAME_DURATION = 5f;
	public const float PAUSE_DURATION = 5f;

	public Minigame CurrentMinigame => CurrentMinigameIndex >= 0 ? Minigames[CurrentMinigameIndex] : null;

	public int PlayerCount => Scene.Components.GetAll<Player>().Count();

	public static SoundHandle BackgroundMusic;

	public float TimeLeft => State switch
	{
		GameState.Intermission => INTERMISSION_DURATION - TimeInState,
		GameState.Playing => (CurrentMinigame?.Duration ?? MINIGAME_DURATION) - TimeInState,
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
		Minigames = Components.GetAll<Minigame>().ToList();
	}

	private void ChangeState( GameState newState )
	{
		State = newState;
		TimeInState = 0;

		switch ( State )
		{
			case GameState.Intermission:
				ReviveAllPlayers();
				ResetWeapons();
				DistributeWeapon( "prefabs/weapons/fists/w_fists.prefab" );

				CurrentMinigameIndex = -1;
				break;
			case GameState.Playing:
				ReviveAllPlayers();
				ResetWeapons();
				StartMinigame();
				CurrentMinigame?.SetWeapon();
				break;
			case GameState.Pause:
				ResetWeapons();
				DistributeWeapon( "prefabs/weapons/fists/w_fists.prefab" );
				break;
		}
	}

	public void ResetWeapons()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			player.Inventory.ResetWeapons();
		}
	}

	public void DistributeWeapon( string weapon )
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			player.Inventory.Pickup( weapon );
		}
	}

	private void StartMinigame()
	{
		if ( Minigames.Count == 0 ) return;

		List<int> availableMinigames = new();

		for ( int i = 0; i < Minigames.Count; i++ )
		{
			if ( !Minigames[i].Requirements() )
				continue;

			availableMinigames.Add( i );
		}

		CurrentMinigameIndex = availableMinigames[Random.Shared.Next( availableMinigames.Count )];
		var minigame = CurrentMinigame;

		RespawnAllPlayers( CurrentMinigame.SpawnGroup );
		minigame.Start();
	}

	int lastTickSound = -1;

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost ) return;

		HandleStateLogic();

		if ( State == GameState.Playing )
		{
			if ( !CurrentMinigame.WaitTillEnd )
			{
				bool playerCouldStillWin = false;

				foreach ( var player in Scene.GetAllComponents<Player>() )
				{
					if ( !player.IsDead && !CurrentMinigame.WinCondition( player ) )
						playerCouldStillWin = true;
				}

				if ( !playerCouldStillWin )
					TimeInState = CurrentMinigame.Duration;
			}

			int currentSecond = (int)TimeInState;
			float timeLeft = CurrentMinigame.Duration - TimeInState;

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

	[Rpc.Broadcast]
	private void StopBgm()
	{
		BackgroundMusic?.Stop();
		BackgroundMusic = null;
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
					StopBgm();
					Sound.Play( "new_game" );

					BackgroundMusic = Sound.Play( "exp_loop" );
					ChangeState( GameState.Playing );
				}

				break;
			case GameState.Playing:
				if ( TimeInState >= CurrentMinigame.Duration )
				{
					EvaluateMinigame();

					StopBgm();
					Sound.Play( "new_game" );
					BackgroundMusic = Sound.Play( "exp_loop" );

					CurrentMinigame.OnEnd();
					ChangeState( GameState.Pause );
				}

				CurrentMinigame.FixedUpdate();
				GetWinners();
				break;
			case GameState.Pause:
				if ( TimeInState >= PAUSE_DURATION )
				{
					PlaySound( "start" );
					StopBgm();
					Sound.Play( "new_game" );

					BackgroundMusic = Sound.Play( "exp_loop" );
					ChangeState( GameState.Playing );
				}

				break;
		}
	}

	public void ReviveAllPlayers()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			player.Revive();
		}
	}

	private void EvaluateMinigame()
	{
		var Winners = GetWinners();
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			bool succeeded = Winners.Contains( player );

			CurrentMinigame?.WinEvent( succeeded, player );

			player.Points += succeeded ? 1 : 0;
		}
	}

	private List<Player> GetWinners()
	{
		var minigame = CurrentMinigame;
		if ( minigame == null ) return null;

		var results = new Dictionary<Player, bool>();

		List<Player> players = new();
		var playerComponents = Scene.GetAllComponents<Player>().OrderBy( x => -x.Points );
		foreach ( var player in playerComponents )
		{
			bool succeeded = minigame.WinCondition( player );
			results[player] = succeeded;

			if ( !succeeded )
				continue;

			players.Add( player );
		}

		ResultsDisplay.UpdateResults( results );

		return players;
	}

	public bool HasMinimumPlayers() => Connection.All.Count >= MinPlayers;

	void INetworkListener.OnActive( Connection channel )
	{
		SpawnPlayerForConnection( channel );
	}

	public void SpawnPlayerForConnection( Connection channel )
	{
		var startLocation = FindSpawnLocation().WithScale( 1 );

		var playerGo = GameObject.Clone( "/prefabs/player.prefab",
			new CloneConfig
			{
				Name = $"Player - {channel.DisplayName}", StartEnabled = true, Transform = startLocation
			} );

		var player = playerGo.Components.Get<Player>( true );
		playerGo.NetworkSpawn( channel );

		IPlayerEvent.PostToGameObject( player.GameObject, x => x.OnSpawned() );
	}

	[Rpc.Broadcast]
	private void RespawnAllPlayers( string spawnGroup )
	{
		var startLocation = FindSpawnLocation( spawnGroup ).WithScale( 1 );
		var player = Player.FindLocalPlayer();
		player.WorldTransform = startLocation;
		player.Network.ClearInterpolation();
	}

	private Transform FindSpawnLocation( string spawnGroup = "Main" )
	{
		var spawnPoints = SpawnGroups[0].Children;

		foreach ( var group in SpawnGroups )
		{
			if ( group.Name != spawnGroup )
				continue;

			spawnPoints = group.Children;

			break;
		}

		if ( spawnPoints.Count > 0 )
		{
			return Random.Shared.FromList( spawnPoints ).Transform.World;
		}

		return global::Transform.Zero;
	}

	[Rpc.Broadcast]
	public static void DisplayToast( string text, float duration = 3.0f, Player to = null )
	{
		if ( to != null && Connection.Local != to.Network.Owner )
			return;

		Current.Toast?.AddToast( text, duration );
	}

	[Rpc.Broadcast]
	public static void PlaySound( string sound, Player to = null, string guidTo = null )
	{
		if ( (to != null && Connection.Local != to.Network.Owner) ||
		     (guidTo != null && Connection.Local.Id.ToString() != guidTo) )
			return;

		var soundHandle = Sound.Play( sound );
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
