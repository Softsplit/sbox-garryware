public class SpreadApart : Component, Minigame
{
	public string Name => "Spread Apart!";
	public string Description => $"Stay {Distance} Meters away from anyone.";
	[Property, Title("Distance (Meters)")] public float Distance { get; set; } = 2;
	public bool WaitTillEnd => true;

	public void FixedUpdate()
	{
	}

	public void OnEnd()
	{
	}

	public bool Requirements()
	{
		return GameManager.Current.PlayerCount > 1;
	}

	public void Start()
	{
	}

	public bool WinCondition( Player player )
	{
		var colliders = Scene.FindInPhysics( new Sphere( player.WorldPosition, Distance * 39.37f ) );
		foreach ( var collider in colliders )
		{
			if ( player.GameObject.IsDescendant( collider ) )
				continue;

			if ( collider.Tags.Contains("player") )
				return false;
		}
		return true;
	}
}
