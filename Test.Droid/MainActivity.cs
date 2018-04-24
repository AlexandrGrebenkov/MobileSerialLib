using Android.App;
using Android.OS;

namespace Test.Droid
{
    [Activity(Label = "Test.Droid", MainLauncher = true, Theme = "@android:style/Theme.Light")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            StartActivity(typeof(usb));
        }
    }
}

