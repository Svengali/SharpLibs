







namespace ent
{


	public class DB : db.DB<EntityId, Entity>
	{
	}


	public class Sys : db.System<EntityId, Entity>
	{
		public Sys( res.Ref<db.SystemCfg> cfg, DB db ) 
			: 
			base( cfg, db )
		{
		}
	}


}
