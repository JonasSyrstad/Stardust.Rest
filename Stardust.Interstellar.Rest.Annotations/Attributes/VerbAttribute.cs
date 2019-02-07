using System;

namespace Stardust.Interstellar.Rest.Annotations
{
    public abstract class VerbAttribute : ServiceDescriptionAttribute, IRoute
    {
        public string Route { get; set; }



        protected VerbAttribute(string verb):base("")
        {
            Verb = verb;
        }

        protected VerbAttribute(string verb, string route) : base("")
		{
            Route = route;
            Verb = verb;
        }
	    protected VerbAttribute(string verb, string route,string serviceDescription) : base(serviceDescription)
	    {
		    Route = route;
		    Verb = verb;
	    }
		public string Verb { get; set; }
        string IRoute.Template
        {
            get => Route;
            set { Route = value; }
        }
    }
}