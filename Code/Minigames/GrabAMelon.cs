public class GrabAMelon : Component, Minigame
{
	public string Name => "Grab a Melon!";
	public string Description => "There's not enough for all of us!";

	[Property] private BBox MelonBounds { get; set; }

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected )
			return;
		Gizmo.Draw.LineBBox( MelonBounds );
	}

	public void SetWeapon() => GameManager.Current.DistributeWeapon( "prefabs/weapons/gravgun/w_gravgun.prefab" );

	[Rpc.Broadcast]
	public void OnEnd()
	{
		foreach(var melon in melons)
		{
			melon.GameObject.Destroy();
		}
	}

	List<PropHelper> melons = new();
	public void Start()
	{
		melons = new();

		int playerCount = Scene.GetAllComponents<Player>().Count();

		int targetMelons = Math.Clamp( playerCount / 2, 1, 1000 );

		for ( int i = 0; i < targetMelons; i++ )
		{
			var melon = GameManager.SpawnModel( Cloud.Model( "facepunch/watermelon" ).ResourcePath, MelonBounds.RandomPointInside, Rotation.Random );

			melons.Add( melon );
		}
	}

	public void FixedUpdate()
	{

	}

	public bool WinCondition( Player player )
	{
		foreach ( var melon in melons )
		{
			if ( melon.Network.Owner != player.Network.Owner )
				continue;

			return true;
		}

		return false;
	}
}
