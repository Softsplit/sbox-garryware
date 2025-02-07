public class ReachTheEnd : Component, Minigame
{
	public string Name => "Get to the END!!";
	public string Description => "Reach the fucking  End CUNT!!";
	public string SpawnGroup => "Race";

	public float Duration => 8;

	[Property] private BBox FinishLineBounds { get; set; }

	public void OnEnd()
	{

	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected )
			return;

		Gizmo.Draw.LineBBox( FinishLineBounds );
	}

	private List<Player> InternalSuceeded { get; set; } = new();

	public void Start()
	{
		InternalSuceeded = [];
	}

	public void FixedUpdate()
	{
		if ( GameManager.Current.TimeInState < 1f )
			return;

		InternalSuceeded ??= [];

		foreach ( var player in GameManager.Current.Scene.GetAllComponents<Player>() )
		{
			bool win = WinCondition( player );
			if ( win && !InternalSuceeded.Contains( player ) )
			{
				GameManager.PlaySound( "win", player );
				InternalSuceeded.Add( player );
			}
			if ( !win && InternalSuceeded.Contains( player ) )
			{
				GameManager.PlaySound( "fail", player );
				InternalSuceeded.Remove( player );
			}
		}
	}

	public void WinEvent( bool succeeded, Player player )
	{

		if ( !succeeded )
		{
			player.Kill();
			GameManager.PlaySound( "fail" );
		}

		GameManager.DisplayToast( succeeded ?
			$"You succeeded!" :
			$"You failed!", 2.0f,
			player );
	}

	public bool WinCondition( Player player )
	{
		return FinishLineBounds.Contains( player.WorldPosition + Vector3.Up * 10 );
	}
}
