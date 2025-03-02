using Sandbox;

public class Fireball : Component
{
	[Property] public Player Target { get; set; }

	[Property] public float MoveSpeed { get; set; } = 280f;

	protected override void OnAwake()
	{
		base.OnAwake();

		NetworkSound.ToggleSoundPoint( GameObject, true );
		PickNewTarget();
	}

	private void PickNewTarget()
	{
		Target = Game.Random.FromList( Scene.GetAll<Player>().Where( x => !x.IsDead ).ToList() );
	}

	protected override void OnFixedUpdate()
	{
		if ( !Target.IsValid() )
			return;

		var turnSpeed = 0.01f;

		// Chase target
		var directionToTarget = Target?.Controller?.Renderer?.GetBoneObject( "spine_1" ).WorldPosition - WorldPosition;

		if ( directionToTarget != null )
		{
			var idealRotation = Rotation.LookAt( (Vector3)directionToTarget, Vector3.Up );

			WorldRotation = Rotation.Slerp( WorldRotation, idealRotation, Time.Delta * turnSpeed )
				.Clamp( idealRotation, 0.02f, out _ );
		}

		var nextLocation = WorldPosition + WorldRotation.Forward * MoveSpeed * Time.Delta;

		// Check if we came into contact with a player
		var results = Scene.Trace.Sphere( 12f, WorldPosition, nextLocation )
			.WithTag( "player" )
			.RunAll();

		DebugOverlay.Sphere( new Sphere( WorldPosition, 12f ) );

		if ( results != null )
		{
			foreach ( var tr in results )
			{
				if ( tr.Hit )
				{
					if ( tr.GameObject.Components.TryGet( out Player player ) )
					{
						if ( !player.IsValid() )
							return;

						player.Kill();

						// Pick a new target if we got who we were chasing
						if ( player == Target )
						{
							PickNewTarget();
						}
					}
				}
			}
		}

		// Move into the new location
		WorldPosition = nextLocation;
	}
}

public sealed class AvoidTheFireball : Component, Minigame
{
	public string Name => "Avoid The Fireball!";
	public string Description => "Self explanatory.";
	public float Duration => 9;
	private GameObject Fireball => GameObject.GetPrefab( "prefabs/fireball.prefab" );

	[Property] public BBox Bounds { get; set; }

	public GameObject CurrentFireball { get; set; }

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.LineBBox( Bounds );
	}

	public void Start()
	{
		var fireball = Fireball.Clone();
		CurrentFireball = fireball;
		fireball.WorldPosition = Bounds.RandomPointOnEdge;

		fireball.NetworkSpawn( Connection.Host );
	}

	public void FixedUpdate()
	{
		if ( Scene.GetAll<Player>().All( x => x.IsDead ) )
		{
			CurrentFireball.Destroy();
		}
	}

	public void OnEnd()
	{
		CurrentFireball.Destroy();
	}

	public bool WinCondition( Player player )
	{
		if ( !player.IsDead )
			return true;

		return false;
	}
}
