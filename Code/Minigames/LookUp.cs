public class LookUp : Minigame
{
	public string Name => "Look Up!";
	public string Description => "Look up at the sky!";

	public void OnEnd()
	{

	}

	public void OnStart()
	{
		GameManager.Current.DisplayToast( Description );
	}

	public void OnFixedUpdate()
	{

	}

	public bool WinCondition( Player player )
	{
		return player.EyeTransform.Rotation.Pitch() < -45;
	}
}
