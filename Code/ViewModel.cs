[Icon( "pan_tool" )]
public class ViewModel : Component
{
	[Property] public bool EnableSwingAndBob { get; set; } = true;
	[Property] public float SwingInfluence { get; set; } = 0.05f;
	[Property] public float ReturnSpeed { get; set; } = 5.0f;
	[Property] public float MaxOffsetLength { get; set; } = 10.0f;
	[Property] public float BobCycleTime { get; set; } = 7;
	[Property] public Vector3 BobDirection { get; set; } = new( 0.0f, 1.0f, 0.5f );
	[Property] public float InertiaDamping { get; set; } = 20.0f;

	[RequireComponent] public SkinnedModelRenderer Renderer { get; set; }

	public float YawInertia { get; private set; }
	public float PitchInertia { get; private set; }

	private Vector3 swingOffset;

	private float lastPitch;
	private float lastYaw;
	private float bobAnim;
	private float bobSpeed;

	private bool activated = false;

	protected override void OnEnabled()
	{
		Renderer?.Set( "b_deploy", true );
	}

	protected override void OnPreRender()
	{
		if ( !Player.FindLocalPlayer().IsValid() )
			return;

		var inPos = Scene.Camera.WorldPosition;
		var inRot = Scene.Camera.WorldRotation;

		if ( !activated )
		{
			lastPitch = MathX.Clamp( inRot.Pitch(), -89, 89 );
			lastYaw = inRot.Yaw();

			YawInertia = 0;
			PitchInertia = 0;

			activated = true;
		}

		var cameraBone = Renderer.Model.Bones.GetBone( "camera" );
		if ( cameraBone is not null )
		{
			var cameraBoneIndex = cameraBone.Index;
			if ( cameraBoneIndex != -1 )
			{
				var bone = Renderer.Model.GetBoneTransform( cameraBoneIndex );
				Scene.Camera.WorldPosition += bone.Position;
				Scene.Camera.WorldRotation *= bone.Rotation;
			}
		}

		WorldPosition = inPos;
		WorldRotation = inRot;

		var newPitch = WorldRotation.Pitch();
		var newYaw = WorldRotation.Yaw();

		bool pitchInRange = newPitch < 89f && newPitch > -89f;

		var pitchDelta = pitchInRange ? Angles.NormalizeAngle( newPitch - lastPitch ) : 0;
		var yawDelta = pitchInRange ? Angles.NormalizeAngle( lastYaw - newYaw ) : 0;

		PitchInertia += pitchDelta;
		YawInertia += yawDelta;

		if ( EnableSwingAndBob )
		{
			SwingAndBob( newPitch, pitchDelta, yawDelta );
		}
		else
		{
			Renderer.Set( "aim_yaw_inertia", YawInertia );
			Renderer.Set( "aim_pitch_inertia", PitchInertia );
		}

		if ( pitchInRange )
		{
			lastPitch = newPitch;
			lastYaw = newYaw;
		}

		YawInertia = YawInertia.LerpTo( 0, Time.Delta * InertiaDamping );
		PitchInertia = PitchInertia.LerpTo( 0, Time.Delta * InertiaDamping );
	}

	private void SwingAndBob( float newPitch, float pitchDelta, float yawDelta )
	{
		var player = Player.FindLocalPlayer();
		var playerVelocity = player.Controller.Velocity;

		var verticalDelta = playerVelocity.z * Time.Delta;
		var viewDown = Rotation.FromPitch( newPitch < 89f && newPitch > -89f ? newPitch : lastPitch ).Up * -1.0f;

		verticalDelta *= 1.0f - MathF.Abs( viewDown.Cross( Vector3.Down ).y );

		if ( float.IsNaN( verticalDelta ) )
			return;

		pitchDelta -= verticalDelta * 1.0f;

		if ( float.IsNaN( pitchDelta ) )
			return;

		var speed = playerVelocity.WithZ( 0 ).Length;
		speed = speed > 10.0 ? speed : 0.0f;
		bobSpeed = bobSpeed.LerpTo( speed, Time.Delta * InertiaDamping );

		var offset = CalcSwingOffset( pitchDelta, yawDelta );
		offset += CalcBobbingOffset( bobSpeed );

		WorldPosition += WorldRotation * offset;
	}

	protected Vector3 CalcSwingOffset( float pitchDelta, float yawDelta )
	{
		var swingVelocity = new Vector3( 0, yawDelta, pitchDelta );

		swingOffset -= swingOffset * ReturnSpeed * Time.Delta;
		swingOffset += swingVelocity * SwingInfluence;

		if ( swingOffset.Length > MaxOffsetLength )
			swingOffset = swingOffset.Normal * MaxOffsetLength;

		return swingOffset;
	}

	protected Vector3 CalcBobbingOffset( float speed )
	{
		bobAnim += Time.Delta * BobCycleTime;

		var twoPI = MathF.PI * 2.0f;

		if ( bobAnim > twoPI )
			bobAnim -= twoPI;

		var offset = BobDirection * (speed * 0.005f) * MathF.Cos( bobAnim );
		offset = offset.WithZ( -MathF.Abs( offset.z ) );

		return offset;
	}
}
