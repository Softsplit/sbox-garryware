public sealed class PlayerUse : Component, PlayerController.IEvents
{
	[RequireComponent] public Player Player { get; set; }

	Rigidbody carrying;
	Transform carryTransform;
	Transform carryOriginalTransform;

	public bool Interative;
	public string TooltipIcon;
	public string Tooltip;

	/// <summary>
	/// Hook into the PlayerController's use system and tell it we want to 
	/// interact with RigidBodies that we can carry
	/// </summary>
	Component PlayerController.IEvents.GetUsableComponent( GameObject go )
	{
		var rb = go.GetComponent<Rigidbody>();
		if ( CanCarry( rb ) )
		{
			return rb;
		}

		return default;
	}

	/// <summary>
	/// Can carry rigidbodies that are networked and we can take ownership of
	/// </summary>
	private bool CanCarry( Rigidbody rb )
	{
		if ( !rb.IsValid() ) return false;
		if ( !rb.Network.Active ) return false;
		if ( rb.Network.OwnerTransfer != OwnerTransfer.Takeover ) return false;

		return true;
	}

	void PlayerController.IEvents.StartPressing( Component target )
	{
		var rb = target.GetComponent<Rigidbody>();
		if ( CanCarry( rb ) )
		{
			StartCarry( rb );
		}
	}

	void PlayerController.IEvents.StopPressing( Component target )
	{
		StopCarrying();
	}

	private void StartCarry( Rigidbody rb )
	{
		if ( !rb.Network.TakeOwnership() )
			return;

		StopCarrying();

		carrying = rb;
		carryOriginalTransform = rb.Transform.World;
		carryTransform = Player.EyeTransform.ToLocal( rb.Transform.World );
	}

	void StopCarrying()
	{
		if ( carrying.IsValid() )
		{
			carrying.Network.DropOwnership();
		}

		carrying = default;
	}
	void UpdateTooltips( Component lookingAt, Component pressed )
	{

		if ( !lookingAt.IsValid() || pressed.IsValid() )
		{
			Tooltip = null;
			Interative = false;
			return;
		}

		var tt = lookingAt.GetComponent<Tooltip>();
		if ( tt is not null )
		{
			Tooltip = tt.Text;
			TooltipIcon = tt.Icon;
			Interative = true;
			return;
		}
	}
	
	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		UpdateTooltips( Player.Controller.Hovered, Player.Controller.Pressed );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( carrying.IsValid() && !carrying.IsProxy )
		{
			var targetTransform = Player.EyeTransform.ToWorld( carryTransform );
			targetTransform.Rotation = carryOriginalTransform.Rotation.Angles().WithYaw( targetTransform.Rotation.Angles().yaw );

			var distance = Vector3.DistanceBetween( targetTransform.Position, carrying.WorldPosition );

			if ( distance > 50.0f )
			{
				StopCarrying();
				return;
			}

			var mass = carrying.PhysicsBody.Mass;
			var moveSpeed = mass.Remap( 50, 3000, 0.05f, 2.0f, true );

			carrying.PhysicsBody.SmoothMove( targetTransform, moveSpeed, Time.Delta );
		}
	}
}


public class Tooltip : Component
{
	[Property] public string Text { get; set; }
	[Property, ImageAssetPath] public string Icon { get; set; }
}
