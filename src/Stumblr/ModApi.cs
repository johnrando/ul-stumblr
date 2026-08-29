namespace Stumblr
{
	public class ModApi : IModApi
	{
		public void InitMod(Mod _modInstance)
		{
			Patches.Apply();
		}
	}
}
