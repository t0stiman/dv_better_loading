namespace better_loading;

public static class Utilities
{
	public static float Map(float input, float in_min, float in_max, float out_min, float out_max)
	{
		return (input - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
	}
	
	public record struct MinMax(float minimum, float maximum)
	{
		public readonly float minimum = minimum;
		public readonly float maximum = maximum;
	}
}