using Sandbox.Utility;

public class PopTheBalloons : Component, Minigame
{
	public string Name => "Pop The Balloons!";
	public string Description => $"Pop {BalloonTarget} Balloons!";
	public float Duration => 10;

	[Property] private BBox BalloonBounds { get; set; }
	[Property] private int BalloonTarget { get; set; } = 3;

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

	List<PropDestroyedListener> BalloonPropListeners;

	public void OnEnd()
	{
		foreach ( var propListener in BalloonPropListeners )
		{
			propListener.PropHelper?.Damage( 1000, Guid.Empty );
		}
	}

	public void SetWeapon() => GameManager.Current.DistributeWeapon( "prefabs/weapons/shotgun/w_shotgun.prefab" );

	public void Start()
	{
		GameManager.DisplayToast( Description );

		BalloonPropListeners = new();

		for ( int i = 0; i < BalloonTarget * Connection.All.Count; i++ )
		{
			PropDestroyedListener listener = new( GameManager.SpawnModel( Balloons[Game.Random.Next( 0, Balloons.Count )], BalloonBounds.RandomPointInside, Rotation.Random ) );

			listener.PropHelper.Gravity = false;

			listener.PropHelper.Tint = BalloonColours[Game.Random.Next( 0, BalloonColours.Count )];

			BalloonPropListeners.Add( listener );
		}
	}

	public void FixedUpdate()
	{

	}

	Dictionary<Guid, int> PlayerBalloonsPopped()
	{
		Dictionary<Guid, int> playerBalloonsPopped = new();

		foreach ( var propDestroyedListener in BalloonPropListeners )
		{
			if ( !propDestroyedListener.Destroyed )
				continue;

			playerBalloonsPopped.TryAdd( propDestroyedListener.Attacker, 0 );

			playerBalloonsPopped[propDestroyedListener.Attacker]++;
		}

		return playerBalloonsPopped;
	}

	public bool WinCondition( Player player )
	{
		var playerBalloonsPopped = PlayerBalloonsPopped();

		if ( !playerBalloonsPopped.ContainsKey( player.Network.OwnerId ) )
			return false;

		return playerBalloonsPopped[player.Network.OwnerId] >= BalloonTarget;
	}
}

public class PropDestroyedListener
{
	public PropHelper PropHelper { get; set; }

	public PropDestroyedListener( PropHelper propHelper )
	{
		PropHelper = propHelper;
		PropHelper.OnDamaged += OnDamaged;
	}

	public bool Destroyed { get; set; }

	public Guid Attacker { get; set; }

	public float DestroyedTime { get; set; }

	void OnDamaged( bool destroyed, float amount, Guid attacker )
	{
		DestroyedTime = Time.Now;
		if ( !destroyed )
			return;

		Destroyed = true;

		Attacker = attacker;

		PropHelper = null;
	}
}
