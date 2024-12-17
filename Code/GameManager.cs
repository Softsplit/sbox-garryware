public sealed class GameManager : Component, Component.INetworkListener, ISceneStartup
{
	private enum GameState
	{
		Intermission,
		Round
	}

	[Sync] private GameState CurrentState { get; set; } = GameState.Intermission;
	[Sync] private RealTimeSince TimeSinceStateStart { get; set; }
	[Sync] private int MinPlayers { get; set; } = 2;

	private const float INTERMISSION_DURATION = 30f;
	private const float ROUND_DURATION = 300f; // 5 minutes

	private readonly List<Connection> ActivePlayers = new();

	// Add singleton instance property
	public static GameManager Current { get; private set; }

	protected override void OnEnabled()
	{
		// Set singleton instance when enabled
		Current = this;

		if ( Networking.IsHost )
		{
			Networking.CreateLobby( new Sandbox.Network.LobbyConfig() );

			// Start with intermission
			ChangeState( GameState.Intermission );
		}
	}

	protected override void OnDisabled()
	{
		// Clear singleton instance when disabled
		if ( Current == this )
			Current = null;
	}

	protected override void OnFixedUpdate()
	{
		CheckGameState();
	}

	void INetworkListener.OnActive( Connection channel )
	{
		ActivePlayers.Add( channel );
		SpawnPlayerForConnection( channel );
	}

	void INetworkListener.OnDisconnected( Connection channel )
	{
		ActivePlayers.Remove( channel );
		CheckMinimumPlayers();
	}

	private void CheckGameState()
	{
		if ( !Networking.IsHost ) return;

		switch ( CurrentState )
		{
			case GameState.Intermission when TimeSinceStateStart >= INTERMISSION_DURATION && HasMinimumPlayers():
				StartRound();
				break;
			case GameState.Round when TimeSinceStateStart >= ROUND_DURATION:
				EndRound();
				break;
		}
	}

	private bool HasMinimumPlayers() => ActivePlayers.Count >= MinPlayers;

	private void CheckMinimumPlayers()
	{
		if ( !HasMinimumPlayers() && CurrentState == GameState.Round )
		{
			Log.Info( "Not enough players, returning to intermission" );
			ChangeState( GameState.Intermission );
		}
	}

	[Rpc.Broadcast]
	private void ChangeState( GameState newState )
	{
		CurrentState = newState;
		TimeSinceStateStart = 0;

		switch ( newState )
		{
			case GameState.Intermission:
				Log.Info( $"Intermission started! Waiting {INTERMISSION_DURATION} seconds..." );
				break;
			case GameState.Round:
				Log.Info( $"Round started! Duration: {ROUND_DURATION} seconds" );
				break;
		}
	}

	private void StartRound()
	{
		ChangeState( GameState.Round );
		RespawnAllPlayers();
	}

	private void EndRound()
	{
		ChangeState( GameState.Intermission );
		RespawnAllPlayers();
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
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			var startLocation = FindSpawnLocation().WithScale( 1 );
			player.GameObject.WorldTransform = startLocation;
		}
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

	// Developer commands
	[ConCmd( "game_state" )]
	public static void CmdGameState()
	{
		if ( !Networking.IsHost ) return;

		if ( Current == null ) return;

		var state = Current.CurrentState;
		var timeLeft = state == GameState.Intermission ?
			INTERMISSION_DURATION - Current.TimeSinceStateStart :
			ROUND_DURATION - Current.TimeSinceStateStart;

		Log.Info( $"Current State: {state}, Time Left: {timeLeft:F1}s" );
	}

	[ConCmd( "game_force_intermission" )]
	public static void CmdForceIntermission()
	{
		if ( !Networking.IsHost ) return;
		Current?.ChangeState( GameState.Intermission );
	}

	[ConCmd( "game_force_round" )]
	public static void CmdForceRound()
	{
		if ( !Networking.IsHost ) return;
		Current?.StartRound();
	}

	[ConCmd( "game_set_minplayers" )]
	public static void CmdSetMinPlayers( int count )
	{
		if ( !Networking.IsHost ) return;
		if ( Current == null ) return;

		Current.MinPlayers = count;
		Log.Info( $"Minimum players set to {count}" );
	}
}
