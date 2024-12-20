public class DontMove : Minigame
{
	public string Name => "Don't Move!";
	public string Description => "Stop Moving!";

	List<Player> MovedPlayers;
	public void OnEnd()
	{

	}

	public void OnStart()
	{
		MovedPlayers = new();
		GameManager.Current.DisplayToast( "Stop Moving!" );
	}

	public void OnFixedUpdate()
	{
		if ( GameManager.Current.TimeInState < 0.5f )
			return;

		if ( MovedPlayers == null )
			MovedPlayers = new();

		foreach(var player in GameManager.Current.Scene.GetAllComponents<Player>() )
		{
			if ( player.Controller.Body.Velocity.Length < 0.1f )
				continue;

			MovedPlayers.Add( player );
		}
	}

	public bool WinCondition( Player player )
	{
		return !MovedPlayers.Contains(player);
	}
}
