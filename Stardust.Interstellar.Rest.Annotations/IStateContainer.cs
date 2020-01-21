namespace Stardust.Interstellar.Rest.Extensions
{
	public interface IStateContainer : IExtrasContainer
	{
		Extras Extras { get; }
		bool ContainsKey(string key);
		void Add(string key, object startNew);
	}
}