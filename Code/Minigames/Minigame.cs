public interface Minigame
{
	public string Name => "";
	public string Description => "";
	public float Duration => GameManager.MINIGAME_DURATION;
	public void OnStart();
	public void OnFixedUpdate();
	public void OnEnd();
	public bool WinCondition( Player player );
}
