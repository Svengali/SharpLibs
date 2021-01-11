
using Optional;

using Vec = MathSharp.Vector;
using Vec4 = System.Runtime.Intrinsics.Vector128<float>;



namespace ent
{
	public partial class Component<T>
	{

	}

	public partial class ComWithCfg<T, TCFG> : Component<T>
	{

	}

	public partial class ComHealthCfg : lib.Config
	{
		public float MaxHealth = 100.0f;
	}

	public partial class ComHealth : ComWithCfg<ComHealth, ComHealthCfg>
	{
		public bool isDead()
		{
			return m_health <= 0.0f;
		}

		//[gen.NetView( new[] { typeof( net.View<Gameplay> ) } )]
		public ComHealth takeDamage( Entity ent, float amount )
		{
			var newHealth = m_health - amount;
			var com = with( m_healthOpt: newHealth.Some() );

			return com;
		}
	}


	public partial class ComPhysicsCfg : lib.Config
	{
		public float MaxVelocity = 100.0f;
	}

	public partial class ComPhysics : ComWithCfg<ComPhysics, ComPhysicsCfg>
	{


		public ComPhysics move()
		{
			

			return this;
		}


	}




}

