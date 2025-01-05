using System;

[Spawnable, Library( "weapon_piss" )]
partial class Piss : BaseWeapon
{
	[Property] private LineRenderer LineRenderer { get; set; }
	[Property] private Material Decal { get; set; }
	[Property] private float PissForce { get; set; } = 100f;


	GameObject PissParent;
	protected override void OnStart()
	{
		base.OnStart();
		PissParent = new GameObject();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( Owner?.Controller?.Renderer?.TryGetBoneTransform( "pelvis", out var pelvisTransform ) ?? false )
		{
			Gizmo.Draw.Color = Color.Black;
			Gizmo.Draw.SolidSphere( pelvisTransform.Position, 10 );
		}
	}

	public override bool CanReload()
	{
		return false;
	}

	public override void OnControl()
	{
		base.OnControl();

		var pissPoints = PissParent.Children;

		pissPoints.Reverse();

		var pissVectors = new List<Vector3>();

		foreach ( var piss in pissPoints )
		{
			if ( !piss.IsValid() )
				continue;
			pissVectors.Add( piss.WorldPosition );
		}

		BroadcastPiss( pissVectors );

		for ( int i = 1; i < pissPoints.Count; i++ )
		{
			var trace = TraceBullet( pissPoints[i - 1].WorldPosition, pissPoints[i].WorldPosition, 0.2f );

			foreach ( var hit in trace )
			{
				Log.Info( hit.GameObject );
				Spatter( hit );
			}

			if(trace.Count() > 0)
			{
				pissPoints[i].Destroy();
			}
		}
	}

	private void Spatter(SceneTraceResult tr)
	{
		var go = new GameObject
		{
			Name = "PISS PUDDLE!",
			Parent = tr.GameObject,
			WorldPosition = tr.EndPosition,
			WorldRotation = Rotation.LookAt( -tr.Normal )
		};

		if ( tr.Bone > -1 )
		{
			var renderer = tr.GameObject.GetComponentInChildren<SkinnedModelRenderer>();
			var bone = renderer.GetBoneObject( tr.Bone );

			go.SetParent( bone );
		}

		var decalRenderer = go.AddComponent<DecalRenderer>();
		decalRenderer.Material = Decal;
		decalRenderer.Size = Vector3.One*50;

		go.NetworkSpawn( null );
		go.Network.SetOrphanedMode( NetworkOrphaned.Host );
		go.DestroyAsync( 10f );
	}

	[Rpc.Broadcast]
	private void BroadcastPiss(List<Vector3> pissPoints)
	{
		LineRenderer.VectorPoints = pissPoints;
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		if (Owner?.Controller?.Renderer?.TryGetBoneTransform( "pelvis", out var pelvisTransform ) ?? false)
		{
			var pissPoint = new GameObject();
			pissPoint.WorldPosition = pelvisTransform.Position;
			var pissBody = pissPoint.AddComponent<Rigidbody>();
			pissBody.ApplyForce( (Owner.AimRay.Forward + Vector3.Up * 0.5f) * PissForce );
			pissPoint.SetParent( PissParent );
			pissPoint.DestroyAsync( 5 );
		}
	}
}

