using static Sandbox.Component;

/// <summary>
/// Holds player information like health
/// </summary>
public sealed partial class Player : Component, IDamageable, PlayerController.IEvents
{
	[Sync( SyncFlags.FromHost )]
	public int Points { get; set; }
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

	ModelHitboxes _hitboxes;
	public ModelHitboxes Hitboxes 
	{ 
		get 
		{
			if(_hitboxes == null)
				_hitboxes = GetComponent<ModelHitboxes>();
			return _hitboxes; 
		} 
	}

	[Property]
	public GameObject Body { get; set; }

	float health = 100;
	[Property, Range( 0, 100 )]
	[Sync] public float Health 
	{ 
		get { return health; } 
		set 
		{
			bool alreadyDead = health <= 0;

			health = value;

			if(health <= 0 && !alreadyDead)
				Gib();
		}
	}

	[Sync] public bool IsSitting { get; set; } = false;

	public bool IsDead => Health <= 0;

	public Transform EyeTransform => Controller.EyeTransform;

	public Ray AimRay => new( EyeTransform.Position, EyeTransform.Rotation.Forward );

	public event Action<bool, float, Guid> OnDamaged;

	bool thirdPerson;
	bool movementSettingsSaved = false;

	bool wasDead;
	protected override void OnFixedUpdate()
	{

		if ( IsDead )
		{
			Controller.ThirdPerson = true;
			Controller.DuckedHeight = Controller.BodyHeight;
		}

		Body.Enabled = !IsDead;

		Controller.ColliderObject.Enabled = !IsDead;
		Controller.Body.Enabled = !IsDead;
		Hitboxes.Enabled = !IsDead;
	}
	private void Gib()
	{
		GameObject go = new GameObject();

		go.WorldTransform = Body.WorldTransform;

		var prop = go.Components.Create<Prop>();

		prop.Model = Model.Load( "models/citizen_gibs/citizen_gib.vmdl" );

		var modelRenderer = go.GetComponent<SkinnedModelRenderer>();

		modelRenderer.CreateBoneObjects = true;

		var myRenderer = Body.Components.Get<SkinnedModelRenderer>();

		if ( !myRenderer.IsValid() )
			return;

		for(int i = 0; i < myRenderer.GetBoneTransforms(true).Count(); i++ )
		{
			if ( !modelRenderer.GetBoneObject( i ).IsValid() || !myRenderer.GetBoneObject( i ).IsValid() )
				continue;

			modelRenderer.GetBoneObject(i).WorldTransform = myRenderer.GetBoneObject(i).WorldTransform;
		}

		SandboxBaseExtensions.CreateParticle( "particles/impact.flesh.bloodpuff-big.vpcf", null, WorldPosition + Vector3.Up * Controller.BodyHeight / 2, Rotation.Identity, false );

		var sound = ResourceLibrary.Get<SoundEvent>( "sounds/impacts/bullets/impact-bullet-flesh.sound" );

		sound.Volume = 100;

		Sound.Play( sound, WorldPosition );

		var gibs = prop.CreateGibs();

		foreach(var gib in gibs)
		{
			var rb = gib.GameObject.GetComponent<Rigidbody>();

			if ( !rb.IsValid() )
				continue;

			rb.ApplyImpulse( (gib.WorldPosition - WorldPosition).Normal * 5000 * rb.Mass);
		}

		prop.GameObject.Destroy();

		go.Destroy();
	}

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

	[Rpc.Broadcast]
	public void Kill()
	{
		if ( IsProxy ) return;

		if ( IsDead ) return;

		thirdPerson = Controller.ThirdPerson;

		movementSettingsSaved = true;
		Health = 0;
	}

	[Rpc.Broadcast]
	public void Revive()
	{
		if ( IsProxy ) return;

		if(movementSettingsSaved)
		{
			Controller.ThirdPerson = thirdPerson;
		}

		Health = 100;
	}

	public Guid lastAttacker;

	[Rpc.Broadcast]
	public void TakeDamage( float amount, Guid attacker )
	{
		if ( Health <= 0 ) return;

		lastAttacker = attacker;

		if ( !IsProxy ) Health -= amount;

		OnDamaged?.Invoke( Health <= 0f, amount, attacker );

		IPlayerEvent.PostToGameObject( GameObject, x => x.OnTakeDamage( amount ) );

		if ( !IsProxy && Health <= 0 )
		{
			Health = 0;
			//Kill( attacker );
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
		TakeDamage( damage.Damage, Guid.Empty );
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
