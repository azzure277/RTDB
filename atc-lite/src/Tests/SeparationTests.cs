using Shared.Models;
using Xunit;

public class SeparationTests
{
	[Theory]
	[InlineData(WakeClass.Heavy, WakeClass.Light, 6)]
	[InlineData(WakeClass.Heavy, WakeClass.Medium, 5)]
	[InlineData(WakeClass.Heavy, WakeClass.Heavy, 4)]
	[InlineData(WakeClass.Medium, WakeClass.Light, 5)]
	[InlineData(WakeClass.Medium, WakeClass.Medium, 3)]
	[InlineData(WakeClass.Light, WakeClass.Light, 3)]
	public void MinimaTable_IsCorrect(WakeClass leader, WakeClass follower, int expectedNm)
	{
		int actual = WakeMinima.RequiredNm(leader, follower);
		Assert.Equal(expectedNm, actual);
	}
}
