[Title( "Don't Move" )]
public class DontMove : Component, Minigame
{
	public string Name => "Don't Move!";
	public string Description => "Stop moving!";

	private List<Player> MovedPlayers { get; set; } = [];

	public void OnEnd()
	{

	}

	public void Start()
	{
		MovedPlayers = [];
	}

	public void FixedUpdate()
	{
		if ( GameManager.Current.TimeInState < 0.5f )
			return;

		MovedPlayers ??= [];

		foreach ( var player in GameManager.Current.Scene.GetAllComponents<Player>() )
		{
			if ( player.Controller.WishVelocity.Length < 1f )
				continue;

			if ( !MovedPlayers.Contains( player ) )
			{
				GameManager.PlaySound( "fail", player );
				MovedPlayers.Add( player );
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
		return !MovedPlayers.Contains( player );
	}
}
