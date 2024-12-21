public class HotMelon : Component, Minigame
{
	public string Name => "Hot Melon!";
	public string Description => "NO IDEA!";

	public float Duration => 1000f;

	public void SetWeapon() => GameManager.Current.DistributeWeapon( "prefabs/weapons/gravgun/w_gravgun.prefab" );

	public void OnEnd()
	{

	}

	public void Start()
	{
		GameManager.SpawnModel( "models/citizen_props/roadcone01.vmdl_c", Vector3.Up*20, Rotation.Random );
	}

	public void FixedUpdate()
	{

	}

	public bool WinCondition( Player player )
	{
		return false;
	}
}
