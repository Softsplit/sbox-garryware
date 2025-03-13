public class PlayerCameraEffects : Component, IPlayerEvent, ILocalPlayerEvent
{
	[RequireComponent] public Player Player { get; set; }

	void ILocalPlayerEvent.OnCameraPostSetup( CameraComponent camera )
	{
		if ( IsProxy ) return;

		MovementEffects( camera );
	}

	private void MovementEffects( CameraComponent camera )
	{
		camera.FieldOfView = Screen.CreateVerticalFieldOfView( Preferences.FieldOfView );
	}
}
