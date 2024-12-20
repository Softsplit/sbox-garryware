public interface Minigame
{
	public string Name => "";
	public string Description => "";
	public float Duration => GameManager.MINIGAME_DURATION;
	public void Start();
	public void FixedUpdate();
	public void OnEnd();
	public bool WinCondition( Player player );
}
