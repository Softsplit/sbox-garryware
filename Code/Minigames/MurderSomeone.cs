public class MurderSomeone : Component, Minigame
{
	public string Name => "Murder Someone!";
	public string Description => "Someones got to die";

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
		foreach ( var p in Scene.GetAllComponents<Player>() )
		{
			if ( p.IsDead && p.lastAttacker == player.Network.OwnerId)
				return true;
		}
		return false;
	}
}
