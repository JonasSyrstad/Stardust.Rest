using System;
using System.Collections.Generic;

namespace Stardust.Interstellar.Rest.Extensions
{
	[Serializable]
    public class Extras : Dictionary<string, object>,IExtrasContainer
    {
        public T GetState<T>(string key)
        {
            object state;
            if (!TryGetValue(key, out state)) return default(T);
            if (state != null) return (T)state;
            return default(T);
        }

        public Extras SetState<T>(string key, T value)
        {
            if (ContainsKey(key)) return this;
            Add(key, value);
            return this;
        }
    }
}