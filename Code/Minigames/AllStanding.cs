public class AllStanding : Component, Minigame
{
	public string Name => "Everyone Calm Down!";
	public string Description => "Do NOT kill ANYONE!";

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


	public void FixedUpdate()
	{

	}

	public bool WinCondition( Player player )
	{
		foreach( var p in Scene.GetAllComponents<Player>() )
		{
			if ( p.IsDead )
				return false;
		}
		return true;
	}
}
