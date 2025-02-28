
public class Chair : Component, Component.IPressable
{
	public Player Occupant { get; set; }

	bool wasThirdPerson;
	
	[Rpc.Broadcast]
	public void Occupy( GameObject user )
	{
		if ( user.Network.Owner != Rpc.Caller )
			return;

		if ( IsProxy )
			return;

		Occupant = user.GetComponent<Player>();
		if ( Occupant.IsValid() )
		{
			wasThirdPerson = Occupant.Controller.ThirdPerson;
			Occupant.GameObject.Parent = GameObject;
			Occupant.CurrentChair = this;
			Occupant.LocalPosition = new Vector3( 7, 0, 7 );
			Occupant.Controller.Body.PhysicsBody.Enabled = false;
			Occupant.Controller.ThirdPerson = true;
		}

	}

	[Rpc.Broadcast]
	public void RemoveOccupant()
	{
		if ( !Occupant.IsValid() )
			return;

		var position = Occupant.WorldPosition;
		Occupant.GameObject.Parent = Scene;
		Occupant.WorldPosition = position + Vector3.Up*7;
		Occupant.CurrentChair = null;
		Occupant.Controller.Body.PhysicsBody.Enabled = true;
		Occupant.Controller.ThirdPerson = wasThirdPerson;
	}

	protected override void OnDestroy()
	{
		RemoveOccupant();
	}

	public bool Press( IPressable.Event e )
	{
		if ( GameObject.Children.Count > 0 )
			return false;

		Occupy( e.Source.GameObject );

		return true;
	}

	public bool CanPress( IPressable.Event e )
	{

		return true;
	}
}
