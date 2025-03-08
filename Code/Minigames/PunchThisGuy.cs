public class PunchThisGuy : Component, Minigame
{
	public string Name => Player.FindLocalPlayer() == Punched ? $"Don't Get Punched" : $"Punch\n{Punched.Network.Owner.DisplayName}!";
	public string Description => Player.FindLocalPlayer() == Punched ? "Everyone wants to punch you, good luck." : "This guy sucks, punch him!";

	public float Duration = 10;

	[Sync] private Player Punched { get; set; }

	private MinigameUtilities.PlayerDamageListener PlayerListener;

	public bool Requirements()
	{
		return GameManager.Current.PlayerCount > 1;
	}

	public void Start()
	{
		var players = Scene.GetAllComponents<Player>();
		Punched = players.ElementAt( Random.Shared.Next( players.Count() ) );
		PlayerListener = new( Punched );
		internalSucceeded = new();
	}

	public void OnEnd()
	{
		if ( !Punched.IsValid() )
			GameManager.DisplayToast( "Punched player left... what a loser!" );
	}

	List<Player> internalSucceeded;
	public void FixedUpdate()
	{
		if ( !Punched.IsValid() )
			Duration = 0;

		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			var attackers = PlayerListener.Attackers.Keys.ToList();

			if ( attackers.Contains( player.Network.OwnerId ) && !internalSucceeded.Contains( player ) )
			{
				internalSucceeded.Add( player );
				GameManager.PlaySound( "win", player );
			}
		}
	}

	public void WinEvent( bool succeeded, Player player )
	{
		if ( player == Punched && succeeded )
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
		var attackers = PlayerListener.Attackers.Keys.ToList();

		Log.Info( attackers.Count );

		return player == Punched ? attackers.Count <= 0 : attackers.Contains( player.Network.OwnerId );
	}
}
