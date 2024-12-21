public interface Minigame
{
	public string Name => "";
	public string Description => "";
	public float Duration => GameManager.MINIGAME_DURATION;
	public void Start();
	public void FixedUpdate();
	public void OnEnd();
	public void WinEvent( bool succeeded, Player player )
	{
		GameManager.PlaySound( succeeded ? "win" : "fail", player );

		GameManager.DisplayToast( succeeded ?
			$"You succeeded!" :
			$"You failed!", 2.0f,
			player );
	}
	public void SetWeapon() => GameManager.Current.DistributeWeapon( "prefabs/weapons/fists/w_fists.prefab" );
	public bool WinCondition( Player player );
}
