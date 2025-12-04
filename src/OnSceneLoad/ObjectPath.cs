public record struct ObjectPath
{
	public readonly string fullPath;
		
	public readonly string scene;
	public readonly string rootObject;
	public readonly string subPath;

	public ObjectPath(string fullPath_)
	{
		fullPath = fullPath_;

		var slashIndex = fullPath_.IndexOf('/');
		rootObject = fullPath.Substring(0, slashIndex);
		subPath = fullPath.Substring(slashIndex+1);
			
		scene = rootObject.Replace("_LFS", "");
	}

	public static implicit operator ObjectPath(string fullPath)
	{
		return new ObjectPath(fullPath);
	}
}