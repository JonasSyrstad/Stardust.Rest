using System;
using System.Diagnostics;

namespace Stardust.Interstellar.Rest.Extensions
{
	[Serializable]
    public class StateDictionary : Extras, IStateContainer
    {
        public const string StardustExtras = "stardust.extras";

        /// <summary>
        /// Contains additional information passed to the client application 
        /// </summary>
        public Extras Extras => GetState<Extras>(StardustExtras);
    }
}   