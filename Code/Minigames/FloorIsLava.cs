public class FloorIsLava : Component, Minigame
{
	public string Name => "Floor is Lava!";
	public string Description => "Stay Off The Floor!";
	[Property] public float FloorLevel { get; set; } = 10f;

	public void OnEnd()
	{

	}
	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected )
			return;
		Gizmo.Draw.Plane(WorldPosition.WithZ(FloorLevel), Vector3.Up);
	}

	private List<Player> BurntPlayers { get; set; } = new();

	public void Start()
	{
		GameManager.DisplayToast( Description );
		BurntPlayers = new();
	}

	public void FixedUpdate()
	{
		if ( GameManager.Current.TimeInState < 3f )
			return;

		BurntPlayers ??= new();

		foreach ( var player in GameManager.Current.Scene.GetAllComponents<Player>() )
		{
			if ( player.WorldPosition.z > FloorLevel )
				continue;

			if ( !BurntPlayers.Contains(player) )
			{
				GameManager.PlaySound( "fail", player );
				BurntPlayers.Add( player );
			}
		}
	}

	public void FailedEvent( bool succeeded, Player player )
	{
		if(succeeded)
			GameManager.PlaySound( "win" );

		GameManager.DisplayToast( succeeded ?
			$"You succeeded!" :
			$"You failed!", 2.0f,
			player );
	}

	public bool WinCondition( Player player )
	{
		return !BurntPlayers.Contains( player );
	}
}
