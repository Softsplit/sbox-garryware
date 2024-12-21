public class LookUp : Component, Minigame
{
	public string Name => "Look Up!";
	public string Description => "Look up at the sky!";

	public void OnEnd()
	{

	}

	public void Start()
	{
		GameManager.DisplayToast( Description );
	}

	public void FixedUpdate()
	{

	}

	public bool WinCondition( Player player )
	{
		return player.EyeTransform.Rotation.Pitch() < -45;
	}
}
