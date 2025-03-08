public class TheFloorIsLava : Component, Minigame
{
	public string Name => "The Floor Is Lava!";
	public string Description => "Stay off the floor!";

	[Property] public float FloorLevel { get; set; } = 10f;

	public void OnEnd()
	{

	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected )
			return;

		Gizmo.Draw.Plane( WorldPosition.WithZ( FloorLevel ), Vector3.Up );
	}

	private List<Player> BurntPlayers { get; set; } = new();

	public void Start()
	{
		BurntPlayers = [];
	}

	public void FixedUpdate()
	{
		if ( GameManager.Current.TimeInState < 1f )
			return;

		BurntPlayers ??= [];

		foreach ( var player in GameManager.Current.Scene.GetAllComponents<Player>() )
		{
			if ( player.WorldPosition.z > FloorLevel )
				continue;

			if ( !BurntPlayers.Contains( player ) )
			{
				GameManager.PlaySound( "fail", player );
				BurntPlayers.Add( player );
				player.Kill();
			}
		}
	}

	public void WinEvent( bool succeeded, Player player )
	{
		if ( succeeded )
			GameManager.PlaySound( "win" );

		if ( !succeeded )
			player.Kill();

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
