public class LastManStanding : Component, Minigame
{
	public string Name => "Last Man Standing!";
	public string Description => "Be the last man standing!";

	public float Duration = 10;

	public void SetWeapon() => GameManager.Current.DistributeWeapon( "prefabs/weapons/fists/w_dangerousfists.prefab" );

	public bool Requirements()
	{
		return GameManager.Current.PlayerCount > 1;
	}

	public void Start()
	{

	}

	public void OnEnd()
	{

	}

	List<Player> internalSucceeded;
	public void FixedUpdate()
	{
		foreach ( var player in Scene.GetAllComponents<Player>() )
		{
			if ( !WinCondition( player ) )
				continue;

			if ( !internalSucceeded.Contains( player ) )
			{
				internalSucceeded.Add( player );
				GameManager.PlaySound( "win", player );
			}
		}
	}

	public void WinEvent( bool succeeded, Player player )
	{
		if ( !succeeded )
		{
			player.Kill();
			GameManager.PlaySound( "fail", player );
		}

		GameManager.DisplayToast( succeeded ?
			$"You succeeded!" :
			$"You failed!", 2.0f,
			player );
	}

	public bool WinCondition( Player player )
	{
		foreach( var p in Scene.GetAllComponents<Player>() )
		{
			if ( p != player && !p.IsDead )
				return false;
		}
		return !player.IsDead;
	}
}
