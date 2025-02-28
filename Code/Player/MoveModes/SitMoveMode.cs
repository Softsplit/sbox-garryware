
using Sandbox.Movement;

[Icon( "scuba_diving" ), Group( "Movement" ), Title( "MoveMode - Sitting" )]
public class SitMoveMode : MoveMode
{
	[RequireComponent]
	private Player Player { get; set; }
	
	public override int Score( PlayerController controller )
	{
		if ( Player.CurrentChair.IsValid() ) return 20;

		return -100;
	}

	public override void OnModeBegin()
	{
		Controller.IsClimbing = true;
		Controller.Renderer.Set( "sit", 1 );
	}

	protected override void OnPreRender()
	{
		if ( Controller.Mode != this || !Player.CurrentChair.IsValid() )
			return;

		Controller.Renderer.WorldRotation = Angles.Zero.WithYaw( Player.CurrentChair.WorldRotation.Yaw() );

		if(Input.Down("jump") || Input.Down("duck"))
			Player.CurrentChair.RemoveOccupant();
	}

	public override void OnModeEnd( MoveMode next )
	{
		Controller.IsClimbing = false;
		Controller.Renderer.Set( "sit", 0 );
	}
}
