public sealed class GameManager : GameObjectSystem<GameManager>, Component.INetworkListener, ISceneStartup
{
	public GameManager( Scene scene ) : base( scene )
	{
	}

	public enum RoundState
	{
		WaitingForPlayers,
		Intermission,
		RoundActive
	}

	public float IntermissionDuration => 30.0f;
	public float RoundDuration => 300.0f;

	public int MinimumPlayers { get; set; } = 2;
	private RoundState CurrentRoundState { get; set; } = RoundState.Intermission;

	void ISceneStartup.OnHostPreInitialize( SceneFile scene )
	{
		Log.Info( $"Walker: Loading scene {scene.ResourceName}" );
	}

	void ISceneStartup.OnHostInitialize()
	{
		// Existing code
		var slo = new SceneLoadOptions();
		slo.IsAdditive = true;
		slo.SetScene( "scenes/engine.scene" );
		Scene.Load( slo );

		Networking.CreateLobby();

		// Start the game loop asynchronously
		_ = RunGameLoopAsync();
	}

	private async Task RunGameLoopAsync()
	{
		while ( true )
		{
			switch ( CurrentRoundState )
			{
				case RoundState.WaitingForPlayers:
					Log.Info( "Waiting for players..." );
					while ( GetPlayerCount() < MinimumPlayers )
					{
						await GameTask.Delay( 1000 );
					}
					TransitionToState( RoundState.Intermission );
					break;

				case RoundState.Intermission:
					Log.Info( "Intermission has started." );
					RespawnAllPlayersInLobby();
					await GameTask.Delay( (int)(IntermissionDuration * 1000) );
					if ( GetPlayerCount() >= MinimumPlayers )
					{
						TransitionToState( RoundState.RoundActive );
					}
					else
					{
						TransitionToState( RoundState.WaitingForPlayers );
					}
					break;

				case RoundState.RoundActive:
					Log.Info( "Round has started!" );
					RespawnAllPlayersForRound();
					var roundEndTime = Time.Now + RoundDuration;
					while ( Time.Now < roundEndTime )
					{
						if ( GetPlayerCount() < MinimumPlayers )
						{
							break;
						}
						await GameTask.Delay( 1000 );
					}
					TransitionToState( RoundState.Intermission );
					break;
			}
			await GameTask.Delay( 100 ); // Small delay to prevent tight loop
		}
	}

	void TransitionToState( RoundState newState )
	{
		OnLeaveState( CurrentRoundState );
		CurrentRoundState = newState;
		OnEnterState( CurrentRoundState );
	}

	void OnEnterState( RoundState state )
	{
		switch ( state )
		{
			case RoundState.WaitingForPlayers:
				Log.Info( "Waiting for players..." );
				break;

			case RoundState.Intermission:
				Log.Info( "Intermission has started." );
				RespawnAllPlayersInLobby();
				break;

			case RoundState.RoundActive:
				Log.Info( "Round has started!" );
				RespawnAllPlayersForRound();
				break;
		}
	}

	void OnLeaveState( RoundState state )
	{
		// Cleanup if necessary
	}

	void RespawnAllPlayersInLobby()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			if ( player.IsValid() )
			{
				var spawnPoint = FindLobbySpawnLocation();
				player.GameObject.WorldTransform = spawnPoint;
				player.Health = 100;
			}
		}
	}

	void RespawnAllPlayersForRound()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			if ( player.IsValid() )
			{
				var spawnPoint = FindRoundSpawnLocation();
				player.GameObject.WorldTransform = spawnPoint;
				player.Health = 100;
			}
		}
	}

	int GetPlayerCount()
	{
		return Scene.GetAllComponents<Player>().Count();
	}

	Transform FindLobbySpawnLocation()
	{
		// Implement later

		return FindSpawnLocation();
	}

	Transform FindRoundSpawnLocation()
	{
		return FindSpawnLocation();
	}

	Transform FindSpawnLocation()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			return Random.Shared.FromArray( spawnPoints ).Transform.World;
		}

		return Transform.Zero;
	}

	void Component.INetworkListener.OnActive( Connection channel )
	{
		SpawnPlayerForConnection( channel );
	}

	public void SpawnPlayerForConnection( Connection channel )
	{
		var startLocation = FindLobbySpawnLocation().WithScale( 1 );

		var playerGo = GameObject.Clone( "/prefabs/player.prefab", new CloneConfig { Name = $"Player - {channel.DisplayName}", StartEnabled = true, Transform = startLocation } );
		var player = playerGo.Components.Get<Player>( true );
		playerGo.NetworkSpawn( channel );

		IPlayerEvent.PostToGameObject( player.GameObject, x => x.OnSpawned() );
	}

	[ConCmd( "gw_setstate" )]
	public static void SetRoundState( string stateName )
	{
		if ( Current == null ) return;

		if ( Enum.TryParse<RoundState>( stateName, true, out var newState ) )
		{
			Current.TransitionToState( newState );
			Log.Info( $"GameManager: Round state set to {newState}" );
		}
		else
		{
			Log.Error( $"Invalid round state: {stateName}" );
		}
	}

	[ConCmd( "gw_setminplayers" )]
	public static void SetMinimumPlayers( int minPlayers )
	{
		if ( Current == null ) return;

		Current.MinimumPlayers = minPlayers;
		Log.Info( $"GameManager: Minimum players set to {minPlayers}" );
	}
}
