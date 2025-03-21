﻿public interface Minigame
{
	public string Name => "";
	public string Description => "";
	public float Duration => GameManager.MINIGAME_DURATION;
	public string SpawnGroup => "Main";
	public bool WaitTillEnd => true;
	public void Start();
	public void FixedUpdate();
	public void OnEnd();
	public void WinEvent( bool succeeded, Player player )
	{
		GameManager.PlaySound( succeeded ? "win" : "fail", player );

		if ( !succeeded )
			player.Kill();

		GameManager.DisplayToast( succeeded ?
			$"You succeeded!" :
			$"You failed!", 2.0f,
			player );
	}
	public void SetWeapon() => GameManager.Current.DistributeWeapon( "prefabs/weapons/fists/w_fists.prefab" );
	public bool Requirements() => true;
	public bool WinCondition( Player player );
}
