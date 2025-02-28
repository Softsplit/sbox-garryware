
public class Chair : Component, Component.IPressable
{
	public Player Occupant { get; set; }
	
	[Rpc.Broadcast]
	public void Occupy( GameObject user )
	{
		if ( user.Network.Owner != Rpc.Caller )
			return;

		if ( IsProxy )
			return;

		Occupant = user.GetComponent<Player>();
		if ( Occupant.IsValid )
		{
			Occupant.GameObject.Parent = GameObject;
			Occupant.IsSitting = true;
			Occupant.LocalPosition = new Vector3( 7, 0, 7 );
			Occupant.Controller.Body.PhysicsBody.Enabled = false;
			Occupant.Controller.ThirdPerson = true;
		}

	}

	[Rpc.Broadcast]
	private void RemoveOccupant()
	{
		if ( !Occupant.IsValid )
			return;
		
		Occupant.GameObject.Parent = Scene;
		Occupant.IsSitting = false;
		Occupant.Controller.Body.PhysicsBody.Enabled = true;
		Occupant.Controller.ThirdPerson = true;
	}

	protected override void OnDestroy()
	{
		RemoveOccupant();
	}

	public bool Press( IPressable.Event e )
	{

		
		Occupy( e.Source.GameObject );

		return true;
	}

	public bool CanPress( IPressable.Event e )
	{

		return true;
	}
}
