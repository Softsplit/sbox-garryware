using static Sandbox.Component;

/// <summary>
/// Holds player information like health
/// </summary>
public sealed partial class Player : Component, IDamageable, PlayerController.IEvents
{
	public static Player FindLocalPlayer()
	{
		return Game.ActiveScene.GetAllComponents<Player>().Where( x => !x.IsProxy ).FirstOrDefault();
	}

	[RequireComponent]
	public PlayerController Controller { get; set; }

	PlayerInventory _inventory;
	public PlayerInventory Inventory
	{
		get
		{
			if(_inventory == null)
				_inventory = GetComponent<PlayerInventory>();
			return _inventory;
		}
	}

	[Property]
	public GameObject Body { get; set; }

	[Property, Range( 0, 100 )]
	[Sync] public float Health { get; set; } = 100;

	public bool IsDead => Health <= 0;

	public Transform EyeTransform => Controller.EyeTransform;

	public Ray AimRay => new( EyeTransform.Position, EyeTransform.Rotation.Forward );

	/// <summary>
	/// Creates a ragdoll but it isn't enabled
	/// </summary>
	[Rpc.Broadcast]
	void CreateRagdoll()
	{
		var ragdoll = Controller.CreateRagdoll();
		if ( !ragdoll.IsValid() ) return;

		var corpse = ragdoll.AddComponent<PlayerCorpse>();
		corpse.Connection = Network.Owner;
		corpse.Created = DateTime.Now;
	}

	[Rpc.Owner]
	void CreateRagdollAndGhost()
	{
		if ( !Networking.IsHost ) return;

		var go = new GameObject( false, "Observer" );
		go.Components.Create<PlayerObserver>();
		go.NetworkSpawn( Rpc.Caller );
	}

	public void TakeDamage( float amount )
	{
		if ( IsProxy ) return;
		if ( Health <= 0 ) return;

		Health -= amount;

		IPlayerEvent.PostToGameObject( GameObject, x => x.OnTakeDamage( amount ) );

		if ( Health <= 0 )
		{
			Health = 0;
			Death();
		}
	}

	void Death()
	{
		CreateRagdoll();
		CreateRagdollAndGhost();

		IPlayerEvent.PostToGameObject( GameObject, x => x.OnDied() );

		GameObject.Destroy();
	}

	void IDamageable.OnDamage( in DamageInfo damage )
	{
		TakeDamage( damage.Damage );
	}

	void PlayerController.IEvents.OnEyeAngles( ref Angles ang )
	{
		var player = Components.Get<Player>();
		var angles = ang;
		ILocalPlayerEvent.Post( x => x.OnCameraMove( ref angles ) );
		ang = angles;
	}

	void PlayerController.IEvents.PostCameraSetup( CameraComponent camera )
	{
		var player = Components.Get<Player>();
		ILocalPlayerEvent.Post( x => x.OnCameraSetup( camera ) );
		ILocalPlayerEvent.Post( x => x.OnCameraPostSetup( camera ) );
	}

	void PlayerController.IEvents.OnLanded( float distance, Vector3 impactVelocity )
	{
		var player = Components.Get<Player>();
		IPlayerEvent.PostToGameObject( GameObject, x => x.OnLand( distance, impactVelocity ) );
	}

	[Rpc.Broadcast]
	public void ApplyImpulse( Vector3 force )
	{
		if ( IsProxy ) return;

		Controller.Body.ApplyImpulse( force );
	}
}
