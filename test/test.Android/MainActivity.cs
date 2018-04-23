using System;
using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Stardust.Interstellar.Rest.Annotations;
using Stardust.Interstellar.Rest.Client;
using Stardust.Interstellar.Rest.Annotations.Messaging;
using Stardust.Interstellar.Rest.Service;

namespace test.Droid
{
	[Activity (Label = "test", Icon = "@drawable/icon", Theme="@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;
		    var proxy = ProxyFactory.CreateInstance<ITestApi>("https://mydnvglapigwdevtest.azurewebsites.net");
		    proxy.Apply1("My");
            base.OnCreate (bundle);
            
			global::Xamarin.Forms.Forms.Init (this, bundle);
		    var app = new test.App();
		    app.MainPage.Title = "Loaded options through Stardust.Interstellar";
            LoadApplication (app);
		}
	}
    [IRoutePrefix("")]
    public interface ITestApi
    {
        [IRoute("{id}")]
        [Options]
        void Apply1([In(InclutionTypes.Path)] string id);

        //[IRoute("test2/{id}")]
        //[Get]
        //Task<StringWrapper> Apply2([In(InclutionTypes.Path)] string id, [In(InclutionTypes.Path)]string name, [In(InclutionTypes.Header)]string item3);
        
    }
    public class StringWrapper
    {
        public string Value { get; set; }
        public DateTime? NullDateTime { get; set; }
    }
}

