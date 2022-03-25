using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SonarLint.VisualStudio.Integration.UnitTests.Helpers
{
    //This is to make sure normalising the keys done correctly with culture invariant
    //Lower case of SETTINGSKEY in Turkish is not settingskey but settıngskey
    //https://en.wikipedia.org/wiki/Dotted_and_dotless_I 
    public class TurkishCultureScope : IDisposable
    {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;

        public TurkishCultureScope()
        {
            var cultureInfo = new CultureInfo("tr-TR");
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUICulture;
        }
    }
}
