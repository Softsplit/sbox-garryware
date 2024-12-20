using Sandbox.Diagnostics;

public sealed class PlayerInventory : Component, IPlayerEvent
{
	[RequireComponent] public Player Owner { get; set; }

	[Sync] public NetList<BaseWeapon> Weapons { get; set; } = new();
	[Sync] public BaseWeapon ActiveWeapon { get; set; }

	[Rpc.Broadcast]
	public void SetDefaultWeapons()
	{
		if ( IsProxy ) return;

		foreach ( var weapon in Weapons )
			weapon.DestroyGameObject();

		Weapons = new();

		Pickup( "prefabs/weapons/fists/w_fists.prefab" );
		SetActiveSlot( 0 );
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( ActiveWeapon is PhysGun physgun && physgun.Beaming )
			return;

		if ( Input.Pressed( "slot1" ) ) SetActiveSlot( 0 );
		if ( Input.Pressed( "slot2" ) ) SetActiveSlot( 1 );
		if ( Input.Pressed( "slot3" ) ) SetActiveSlot( 2 );
		if ( Input.Pressed( "slot4" ) ) SetActiveSlot( 3 );
		if ( Input.Pressed( "slot5" ) ) SetActiveSlot( 4 );
		if ( Input.Pressed( "slot6" ) ) SetActiveSlot( 5 );
		if ( Input.Pressed( "slot7" ) ) SetActiveSlot( 6 );
		if ( Input.Pressed( "slot8" ) ) SetActiveSlot( 7 );
		if ( Input.Pressed( "slot9" ) ) SetActiveSlot( 8 );

		if ( Input.MouseWheel != 0 ) SwitchActiveSlot( (int)-Input.MouseWheel.y );
	}

	[Rpc.Broadcast]
	public void Pickup( string prefabName )
	{
		if ( IsProxy )
			return;

		var prefab = GameObject.Clone( prefabName, global::Transform.Zero, Owner.Body, false );
		prefab.NetworkSpawn( false, Network.Owner );

		var weapon = prefab.Components.Get<BaseWeapon>( true );
		Assert.NotNull( weapon );

		Weapons.Add( weapon );

		SetActiveSlot( Weapons.Count - 1 );

		IPlayerEvent.PostToGameObject( Owner.GameObject, e => e.OnWeaponAdded( weapon ) );
		ILocalPlayerEvent.Post( e => e.OnWeaponAdded( weapon ) );
	}

	public void SetActiveSlot( int i )
	{
		var weapon = GetSlot( i );
		if ( ActiveWeapon == weapon )
			return;

		if ( weapon == null )
			return;

		if ( ActiveWeapon.IsValid() )
			ActiveWeapon.GameObject.Enabled = false;

		ActiveWeapon = weapon;

		if ( ActiveWeapon.IsValid() )
			ActiveWeapon.GameObject.Enabled = true;
	}

	public BaseWeapon GetSlot( int i )
	{
		if ( Weapons.Count <= i ) return null;
		if ( i < 0 ) return null;

		return Weapons[i];
	}

	public int GetActiveSlot()
	{
		var aw = ActiveWeapon;
		var count = Weapons.Count;

		for ( int i = 0; i < count; i++ )
		{
			if ( Weapons[i] == aw )
				return i;
		}

		return -1;
	}

	public void SwitchActiveSlot( int idelta )
	{
		var count = Weapons.Count;
		if ( count == 0 ) return;

		var slot = GetActiveSlot();
		var nextSlot = slot + idelta;

		while ( nextSlot < 0 ) nextSlot += count;
		while ( nextSlot >= count ) nextSlot -= count;

		SetActiveSlot( nextSlot );
	}

	[Rpc.Broadcast]
	void IPlayerEvent.OnSpawned()
	{
		if ( IsProxy ) return;

		SetDefaultWeapons();
		SetActiveSlot( 0 );
	}

	[Rpc.Broadcast]
	void IPlayerEvent.OnDied()
	{
		if ( IsProxy ) return;

		if ( Weapons.Count <= 0 )
			return;

		foreach ( var weapon in Weapons )
			weapon.DestroyGameObject();
	}
}
