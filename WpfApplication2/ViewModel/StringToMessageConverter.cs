using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WpfApplication2.ViewModel
{
    public class StringToMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //Console.WriteLine("convertBack");
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            

            //Console.WriteLine("Converter ?");
            //throw new NotImplementedException();
            String str = value as string;
            if (str == null)
            {
                //Console.WriteLine("Converter ? Null");
                return null;
            }
            else
            {
                //Console.WriteLine("Converter ? Not Null");
                return new Message { Body = str, Sender = (string)parameter };
            }
        }
    }
}
