public sealed class GameManager : Component, Component.INetworkListener
{
	public enum GameState
	{
		Intermission,
		Round
	}

	public static GameManager Current { get; private set; }

	[Sync( Flags = SyncFlags.FromHost )] public GameState State { get; set; } = GameState.Intermission;
	[Sync( Flags = SyncFlags.FromHost )] public RealTimeSince TimeSinceStateStart { get; set; }
	[Sync( Flags = SyncFlags.FromHost )] public int MinPlayers { get; set; } = 2;

	public const float INTERMISSION_DURATION = 30f;
	public const float ROUND_DURATION = 300f; // 5 minutes

	protected override void OnEnabled()
	{
		Current = this;

		if ( Networking.IsHost )
		{
			Networking.CreateLobby( new Sandbox.Network.LobbyConfig() );

			ChangeState( GameState.Intermission );
		}
	}

	protected override void OnDisabled()
	{
		if ( Current == this )
			Current = null;
	}

	protected override void OnFixedUpdate()
	{
		CheckGameState();
	}

	void INetworkListener.OnActive( Connection channel )
	{
		SpawnPlayerForConnection( channel );
	}

	void INetworkListener.OnDisconnected( Connection channel )
	{
		CheckMinimumPlayers();
	}

	private void CheckGameState()
	{
		if ( !Networking.IsHost ) return;

		if ( State == GameState.Intermission && !HasMinimumPlayers() )
		{
			TimeSinceStateStart = 0;
			return;
		}

		switch ( State )
		{
			case GameState.Intermission when TimeSinceStateStart >= INTERMISSION_DURATION:
				if ( HasMinimumPlayers() )
					StartRound();
				break;
			case GameState.Round when TimeSinceStateStart >= ROUND_DURATION:
				EndRound();
				break;
		}
	}

	public bool HasMinimumPlayers() => Connection.All.Count >= MinPlayers;

	private void CheckMinimumPlayers()
	{
		if ( !HasMinimumPlayers() && State == GameState.Round )
		{
			Log.Info( "Not enough players, returning to intermission" );
			ChangeState( GameState.Intermission );
		}
	}

	private void ChangeState( GameState newState )
	{
		State = newState;
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
			ROUND_DURATION - Current.TimeSinceStateStart;

		Log.Info( $"Current State: {Current.State}, Time Left: {timeLeft:F1}s" );
	}

	[ConCmd( "gw_force_intermission" )]
	public static void CmdForceIntermission()
	{
		if ( !Networking.IsHost ) return;
		Current?.EndRound();
	}

	[ConCmd( "gw_force_round" )]
	public static void CmdForceRound()
	{
		if ( !Networking.IsHost ) return;
		Current?.StartRound();
	}

	[ConCmd( "gw_set_minplayers" )]
	public static void CmdSetMinPlayers( int count )
	{
		if ( !Networking.IsHost ) return;
		if ( Current == null ) return;

		Current.MinPlayers = count;
		Log.Info( $"Minimum players set to {count}" );
	}
}
