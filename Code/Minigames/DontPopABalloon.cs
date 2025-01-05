using Sandbox.Utility;

public class DontPopABalloons : Component, Minigame
{
	public string Name => "Don't Pop A Balloon!";
	public string Description => $"Don't pop a balloon";
	public float Duration => 10;

	[Property] private BBox BalloonBounds { get; set; }
	[Property] private int BalloonCount { get; set; } = 50;

	private List<string> Balloons => new()
	{
		"models/citizen_props/balloonears01.vmdl_c",
		"models/citizen_props/balloonheart01.vmdl_c",
		"models/citizen_props/balloonregular01.vmdl_c",
		"models/citizen_props/balloontall01.vmdl_c"
	};

	private List<Color> BalloonColours => new()
	{
		Color.Red,
		Color.Green,
		Color.Blue,
		Color.Orange,
		Color.Cyan,
		Color.Magenta,
		Color.Yellow
	};

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected )
			return;
		Gizmo.Draw.LineBBox( BalloonBounds );
	}

	List<MinigameUtilities.PropDamageListener> BalloonPropListeners;

	public void OnEnd()
	{
		foreach ( var propListener in BalloonPropListeners )
		{
			if(propListener.PropHelper.IsValid())
				propListener?.PropHelper?.Damage( 1000, Guid.Empty );
		}
	}

	public void SetWeapon() => GameManager.Current.DistributeWeapon( "prefabs/weapons/shotgun/w_shotgun.prefab" );

	public void Start()
	{
		BalloonPropListeners = new();
		internalFailed = new();

		for ( int i = 0; i < BalloonCount; i++ )
		{
			MinigameUtilities.PropDamageListener listener = new( GameManager.SpawnModel( Balloons[Game.Random.Next( 0, Balloons.Count )], BalloonBounds.RandomPointInside, Rotation.Random ) );

			listener.PropHelper.Gravity = false;

			listener.PropHelper.Tint = BalloonColours[Game.Random.Next( 0, BalloonColours.Count )];

			BalloonPropListeners.Add( listener );
		}
	}

	public void FixedUpdate()
	{

	}

	public void WinEvent( bool succeeded, Player player )
	{
		if( succeeded )
			GameManager.PlaySound( "win", player );

		if ( !succeeded )
			player.Kill();

		GameManager.DisplayToast( succeeded ?
			$"You succeeded!" :
			$"You failed!", 2.0f,
			player );
	}

	List<Guid> internalFailed = new();
	Dictionary<Guid, int> PlayerBalloonsPopped()
	{
		Dictionary<Guid, int> playerBalloonsPopped = new();

		foreach ( var propDestroyedListener in BalloonPropListeners )
		{
			if ( !propDestroyedListener.Destroyed )
				continue;

			playerBalloonsPopped.TryAdd( propDestroyedListener.LastAttacker, 0 );

			playerBalloonsPopped[propDestroyedListener.LastAttacker]++;

			if( playerBalloonsPopped[propDestroyedListener.LastAttacker] >= 0 && !internalFailed.Contains(propDestroyedListener.LastAttacker ) )
			{
				internalFailed.Add( propDestroyedListener.LastAttacker );

				GameManager.PlaySound( "fail", null, propDestroyedListener.LastAttacker.ToString() );

				foreach ( var player in GameManager.Current.Scene.GetAllComponents<Player>() )
				{
					if(player.Network.OwnerId == propDestroyedListener.LastAttacker )
						player.Kill();
				}
			}
		}

		return playerBalloonsPopped;
	}

	public bool WinCondition( Player player )
	{
		var playerBalloonsPopped = PlayerBalloonsPopped();

		if ( !playerBalloonsPopped.ContainsKey( player.Network.OwnerId ) )
			return true;

		return playerBalloonsPopped[player.Network.OwnerId] <= 0;
	}
}
