[Title( "Pick the Right Cup" )]
public sealed class PickTheRightCup : Component, Minigame
{
	[Property] private List<cup> Cups { get; set; }
	[Property] private GameObject Ball { get; set; }
	[Property] private GameObject BallCup { get; set; }
	[Property] private float CuppingSpeed { get; set; } = 10f;
	[Property] private Vector3 ShuffleOffset { get; set; } = new Vector3( -9.509f, 0, 0 );
	public string Name => "Pick the Right Cup!";
	public string Description => "After the cups have shuffled, stand on the corresponding platform.";
	public string SpawnGroup => "CupGame";
	public float Duration => 25;
	private class cup
	{
		[Hide] public Vector3 PreviousPos { get; set; }
		[Hide] public float lerp { get; set; }
		[Hide] public float shuffleDir { get; set; } = 1;
		public Vector3 Pos { get; set; }
		public GameObject CupModel { get; set; }
		public BBox Platform { get; set; }
	}

	private enum CupGameStates
	{
		Intro,
		Shuffling,
		Guessing,
		Showing
	}

	private CupGameStates GameState { get; set; }

	RealTimeSince StateStart { get; set; }
	public void Start()
	{
		internalSucceeded = new();
		GameState = CupGameStates.Intro;
		StateStart = 0;
		Ball.Network.AssignOwnership( Connection.Host );
		foreach ( var cup in Cups )
		{
			cup.CupModel.Network.AssignOwnership( Connection.Host );
			cup.PreviousPos = cup.CupModel.WorldPosition;
			if ( cup.CupModel.IsValid() )
				cup.CupModel.WorldPosition = cup.Pos;
		}
	}

	public void FixedUpdate()
	{

		switch ( GameState )
		{
			case CupGameStates.Intro:
				Intro();
				break;
			case CupGameStates.Shuffling:
				Shuffling();
				break;
			case CupGameStates.Guessing:
				Guessing();
				break;
			case CupGameStates.Showing:
				Showing();
				break;
		}

		Ball.WorldPosition = BallCup.WorldPosition.WithZ( Cups[0].Pos.z );

		Log.Info( GameState );
	}

	void Intro()
	{
		PositionCups( Vector3.Up * 256 );
		if ( StateStart > 5 )
		{
			GameState = CupGameStates.Shuffling;
			StateStart = 0;
			LastShuffle = 0;
		}
	}

	RealTimeSince LastShuffle;
	void Shuffling()
	{
		if ( LastShuffle > 0.5f / MathX.Clamp( StateStart / 10, 1, 3 ) )
			shuffle();
		PositionCups();

		if ( StateStart > 10 )
		{
			GameState = CupGameStates.Guessing;
			StateStart = 0;
			ToastNotification.Current.AddToast( "Guess!", 3 );
		}
	}

	void shuffle()
	{
		LastShuffle = 0;
		var randomCup1 = Cups[Game.Random.Next( Cups.Count )];
		var model = randomCup1.CupModel;
		var randomCup2 = Cups[Game.Random.Next( Cups.Count )];

		randomCup1.CupModel = randomCup2.CupModel;
		randomCup2.CupModel = model;

		randomCup1.PreviousPos = randomCup1.CupModel.WorldPosition;
		randomCup1.shuffleDir = 1;
		randomCup1.lerp = randomCup1 == randomCup2 ? 1 : 0;

		randomCup2.PreviousPos = randomCup2.CupModel.WorldPosition;
		randomCup2.shuffleDir = -1;
		randomCup2.lerp = randomCup1 == randomCup2 ? 1 : 0;
	}

	void Guessing()
	{
		PositionCups();
		if ( StateStart > 5 )
		{
			GetWinners();
			GameState = CupGameStates.Showing;
			StateStart = 0;
		}
	}
	List<Player> internalSucceeded = new();
	void GetWinners()
	{
		foreach ( var cup in Cups )
		{
			if ( cup.CupModel == BallCup )
			{
				foreach ( var player in GameManager.Current.Scene.GetAllComponents<Player>() )
				{
					if ( cup.Platform.Contains( player.WorldPosition + Vector3.Up * 10 ) )
						internalSucceeded.Add( player );
				}
			}
		}
	}
	void Showing()
	{
		PositionCups( Vector3.Up * 256 );
	}

	Vector3 lerpedOffset;
	void PositionCups( Vector3 offset = default )
	{
		foreach ( var cup in Cups )
		{
			if ( !cup.CupModel.IsValid() )
				return;

			cup.lerp = MathX.Clamp( cup.lerp + Time.Delta * CuppingSpeed, 0, 1 );

			var shuffleDis = Vector3.DistanceBetween( cup.PreviousPos, cup.Pos ) / 256;

			var targetPos = Vector3.Lerp( cup.PreviousPos, cup.Pos, cup.lerp ) + ShuffleOffset * easeOutQuint( 1 - MathF.Abs( cup.lerp - 0.5f ) * 2 ) * cup.shuffleDir * shuffleDis;

			lerpedOffset = lerpedOffset.LerpTo( offset, CuppingSpeed * 2 * Time.Delta );
			cup.CupModel.WorldPosition = targetPos + lerpedOffset;
		}
	}

	float easeOutQuint( float x )
	{
		return 1 - MathF.Pow( 1 - x, 5 );
	}

	public void OnEnd()
	{
		foreach ( var cup in Cups )
		{
			if ( cup.CupModel.IsValid() )
				cup.CupModel.WorldPosition = cup.Pos;
		}
	}

	public bool WinCondition( Player player )
	{
		return internalSucceeded.Contains( player );
	}

	protected override void DrawGizmos()
	{
		foreach ( var cup in Cups )
		{
			Gizmo.Draw.LineBBox( cup.Platform );
		}
	}
}
