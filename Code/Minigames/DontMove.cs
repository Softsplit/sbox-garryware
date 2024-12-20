public class DontMove : Component, Minigame
{
	public string Name => "Don't Move!";
	public string Description => "Stop moving!";

	private List<Player> MovedPlayers { get; set; } = new();

	public void OnEnd()
	{

	}

	public void Start()
	{
		GameManager.Current.DisplayToast( Description );
		MovedPlayers = new();
	}

	public void FixedUpdate()
	{
		if ( GameManager.Current.TimeInState < 0.5f )
			return;

		MovedPlayers ??= new();

		foreach ( var player in GameManager.Current.Scene.GetAllComponents<Player>() )
		{
			if ( player.Controller.Body.Velocity.Length < 1f )
				continue;

			MovedPlayers.Add( player );
		}
	}

	public bool WinCondition( Player player )
	{
		return !MovedPlayers.Contains( player );
	}
}
