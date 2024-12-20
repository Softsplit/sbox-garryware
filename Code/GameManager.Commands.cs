public sealed partial class GameManager
{
	[ConCmd( "spawn" )]
	public static void Spawn( string modelname )
	{
		var player = Player.FindLocalPlayer();
		if ( !player.IsValid() )
			return;

		var tr = Game.ActiveScene.Trace.Ray( player.EyeTransform.Position, player.EyeTransform.Position + player.EyeTransform.Rotation.Forward * 500 )
			.UseHitboxes()
			.IgnoreGameObjectHierarchy( player.GameObject )
			.Run();

		var modelRotation = Rotation.From( new Angles( 0, player.EyeTransform.Rotation.Angles().yaw, 0 ) ) * Rotation.FromAxis( Vector3.Up, 180 );

		SpawnModel( modelname, tr.EndPosition, modelRotation );
		Sandbox.Services.Stats.Increment( "spawn.model", 1, modelname );
	}

	public static PropHelper SpawnModel( string modelname, Vector3 endPos, Rotation modelRotation )
	{
		if ( Networking.IsClient )
			return null;

		var model = Model.Load( modelname );
		if ( model == null || model.IsError )
			return null;

		var go = new GameObject
		{
			WorldPosition = endPos + Vector3.Down * model.PhysicsBounds.Mins.z,
			WorldRotation = modelRotation,
			Tags = { "solid" }
		};

		var prop = go.AddComponent<Prop>();
		prop.Model = model;

		if ( prop.Components.TryGet<SkinnedModelRenderer>( out var renderer ) )
		{
			renderer.CreateBoneObjects = true;
		}

		var propHelper = go.AddComponent<PropHelper>();

		var rb = propHelper.Rigidbody;
		if ( rb.IsValid() )
		{
			// If there's no physics model, create a simple OBB
			foreach ( var shape in rb.PhysicsBody.Shapes )
			{
				if ( !shape.IsMeshShape )
					continue;

				var newCollider = go.AddComponent<BoxCollider>();
				newCollider.Scale = model.PhysicsBounds.Size;
			}
		}

		go.NetworkSpawn();
		go.Network.SetOrphanedMode( NetworkOrphaned.Host );

		return propHelper;
	}
}
