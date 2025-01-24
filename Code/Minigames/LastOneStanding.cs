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
