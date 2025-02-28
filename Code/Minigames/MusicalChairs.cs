public class MusicalChairs : Component, Minigame
{
	public string Name => "Musical Chairs";
	public string Description => $"Run around in a circle to the music!";

	[Property] public List<SoundEvent> Music { get; set; }

	public float Duration => 9;

	private bool DontStopSprinting;

	[Property] public List<GameObject> FloorSpawnPoints { get; set; }

	public GameObject Chair => GameObject.GetPrefab( "prefabs/chair.prefab" );

	public List<Chair> Chairs = new();

	public async void Start()
	{
		DontStopSprinting = true;
		var music = Sound.Play( Game.Random.FromList( Music ) );
		await GameTask.DelaySeconds( 7 );
		music.Stop();
		DontStopSprinting = false;


		int playerCount = Scene.GetAllComponents<Player>().Count();

		int targetChairs = Math.Clamp( (int)Math.Ceiling( playerCount * Random.Shared.Float( 0.25f, 0.75f ) ),
			1, playerCount );

		for ( int i = 0; i < targetChairs; i++ )
		{
			var chair = Chair
				.Clone( new CloneConfig { Transform = Game.Random.FromList( FloorSpawnPoints ).WorldTransform } );

			chair.NetworkSpawn();

			Chairs.Add( chair.GetComponent<Chair>() );
		}
	}

	public void FixedUpdate()
	{
	}


	[Rpc.Broadcast]
	public async void OnEnd()
	{
		foreach ( var chair in Chairs )
		{
			chair.RemoveOccupant();
			chair.GameObject.Destroy();
		}

		Chairs.Clear();
	}

	public bool WinCondition( Player player )
	{
		if ( !player.CurrentChair.IsValid() )
		{
			return false;
		}

		return true;
	}
}
