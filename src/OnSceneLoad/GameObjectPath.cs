/// <summary>
/// Stores a path to a GameObject
/// </summary>
public record struct GameObjectPath
{
	public readonly string fullPath;
		
	public readonly string scene;
	public readonly string rootObject;
	/// <summary>
	/// the path without scene name and root object
	/// </summary>
	public readonly string subPath;

	public GameObjectPath(string fullPath_)
	{
		fullPath = fullPath_;

		var slashIndex = fullPath_.IndexOf('/');
		rootObject = fullPath.Substring(0, slashIndex);
		subPath = fullPath.Substring(slashIndex+1);
			
		scene = rootObject.Replace("_LFS", "");
	}

	public static implicit operator GameObjectPath(string fullPath)
	{
		return new GameObjectPath(fullPath);
	}
}