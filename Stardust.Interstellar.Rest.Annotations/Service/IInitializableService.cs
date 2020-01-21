namespace Stardust.Interstellar.Rest.Annotations.Service
{
	public interface IInitializableService
	{
		void Initialize(params object[] instances);
	}
}