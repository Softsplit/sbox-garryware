using Sandbox;

public class Fireball : Component, Component.ITriggerListener
{
	public GameObject Target { get; set; }


	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		var sphere = Scene.FindInPhysics( new Sphere( WorldPosition, 1024f ) );

		// DebugOverlay.Sphere( new Sphere( WorldPosition, 1024f ) );

		foreach ( var obj in sphere )
		{
			if ( obj.Components.TryGet<Player>( out var player ) )
			{
				Target = player.GameObject;

				WorldPosition = WorldPosition.LerpTo( Target.WorldPosition + Vector3.Up * 55, Time.Delta * 2 );
				WorldRotation = Rotation.LookAt( GameObject.WorldPosition - Target.WorldPosition );
			}
		}
	}

	void ITriggerListener.OnTriggerEnter( GameObject other )
	{
		if ( other.Components.TryGet<Player>( out var player ) )
		{
			player.Kill();
		}
	}
}

public sealed class AvoidTheFireball : Component, Minigame
{
	public string Name => "Avoid The Fireball!";
	public string Description => "Self explanatory.";
	public float Duration => 5;
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
	}

	public void FixedUpdate()
	{
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
