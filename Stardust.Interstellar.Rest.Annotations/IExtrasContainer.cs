namespace Stardust.Interstellar.Rest.Extensions
{
	public interface IExtrasContainer
	{
		T GetState<T>(string key);

		Extras SetState<T>(string key, T value);
	}
}