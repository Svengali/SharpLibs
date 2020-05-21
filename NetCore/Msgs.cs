using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;



namespace msg
{

	#region HandlerSpecs
	public interface HandleMgr
	{
	}
	#endregion

	[Serializable]
	public class Msg
	{
	}

	#region Startup

	[Serializable]
	public class Login : Msg
	{
		public String username;
		//TODO SECURITY: salt and pepper
		public String password;
	}

	[Serializable]
	public class LoginRes : Msg
	{
		public bool result;
	}

	[Serializable]
	public class Create : Msg
	{
		public String username;
		//TODO SECURITY: salt and pepper
		public String password;
	}

	[Serializable]
	public class CreateRes : Msg
	{
		public bool result;
	}

	/*
	[Serializable]
	public class Begin : Entity
	{
	}
	*/

	[Serializable]
	public class Loaded : Msg
	{
	}

	#endregion

	#region Support
	[Serializable]
	public class RequestFile : Msg
	{
		public string path;
	}

	[Serializable]
	public class SendFile : Msg
	{
		public string path;
		public byte[] file;
	}
	#endregion


	#region Client -> Server

	/*
	[Serializable]
	public class MoveTo : Msg
	{
		public math.Vec3 pos;
		public math.Vec3 vel;
		public float rot;
	}

	[Serializable]
	public class DebugTeleportTo : Msg
	{
		public math.Vec3 pos;
	}

	[Serializable]
	public class Fire : Msg
	{
		//Types are explicitly named.  
		public enum Slot
		{
			Invalid = 0,
			Primary = 1,
			Secondary = 2,
		}

		public Slot slot;
		public math.Vec3 dir;
	}
	#endregion

	#region Server -> Client
	[Serializable]
	public class Entity : Msg
	{
		public uint id;
	}

	[Serializable]
	public class Spawn : Entity, HandleMgr
	{
		public string configpath;
		public math.Vec3 pos;
	}

	[Serializable]
	public class Despawn : Entity, HandleMgr
	{
	}

	[Serializable]
	public class EntMove : Entity, HandleMgr
	{
		public math.Vec3d pos;
		public math.Vec3d vel;
		public float rot;
	}
	*/

	/*
	[Serializable]
	public class EntLoaded : Entity, HandleMgr
	{
	}
	*/
	#endregion










}
