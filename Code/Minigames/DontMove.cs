﻿public class DontMove : Minigame
{
	public string Name => "Don't Move!";
	public string Description => "Stop moving!";

	private List<Player> MovedPlayers { get; set; } = new();

	public void OnEnd()
	{

	}

	public void OnStart()
	{
		GameManager.Current.DisplayToast( Description );
	}

	public void OnFixedUpdate()
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