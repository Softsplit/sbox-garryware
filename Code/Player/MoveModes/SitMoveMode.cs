
using Sandbox.Movement;

[Icon( "scuba_diving" ), Group( "Movement" ), Title( "MoveMode - Sitting" )]
public class SitMoveMode : MoveMode
{
	[RequireComponent]
	private Player Player { get; set; }
	
	public override int Score( PlayerController controller )
	{
		if ( Player.IsSitting ) return 20;

		return -100;
	}

	public override void OnModeBegin()
	{
		Controller.Renderer.Set( "sit", 1 );	
	}
}
