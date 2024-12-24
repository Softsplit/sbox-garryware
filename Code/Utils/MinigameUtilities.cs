public static class MinigameUtilities
{
	public class PropDamageListener
	{
		public PropHelper PropHelper { get; set; }

		public PropDamageListener( PropHelper propHelper )
		{
			PropHelper = propHelper;
			PropHelper.OnDamaged += OnDamaged;
		}

		public bool Destroyed { get; set; }

		public Guid LastAttacker { get; set; }

		public Dictionary<Guid,(float amount, float time)> Attackers { get; set; } = new();

		public float DestroyedTime { get; set; }

		void OnDamaged( bool destroyed, float amount, Guid attacker )
		{
			Attackers.TryAdd( attacker, (0,0) );

			Attackers[attacker] = (Attackers[attacker].amount+amount, Time.Now);

			LastAttacker = attacker;

			if ( destroyed )
			{
				Destroyed = true;
				DestroyedTime = Time.Now;
			}
		}
	}

	public class PlayerDamageListener
	{
		public Player Player { get; set; }

		public PlayerDamageListener ( Player player )
		{
			Player = player;
			Player.OnDamaged -= OnDamaged;
			Player.OnDamaged += OnDamaged;
		}

		public Guid LastAttacker { get; set; }

		public Dictionary<Guid, (float amount, float time)> Attackers { get; set; } = new();

		public float DestroyedTime { get; set; }

		void OnDamaged( bool killed, float amount, Guid attacker )
		{
			Attackers.TryAdd( attacker, (0, 0) );

			Attackers[attacker] = (Attackers[attacker].amount + amount, Time.Now);

			LastAttacker = attacker;
		}
	}
}
