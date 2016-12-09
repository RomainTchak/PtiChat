using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WpfApplication2.ViewModel
{
    /// <summary>
    /// Convertisseur qui convertit le string du texte du message en objet de classe Message dont la propriété Body est le texte.
    /// </summary>
    public class StringToMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            String str = value as string;
            if (str == null)
            {
                return null;
            }
            else
            {
                return new Message { Body = str, Sender = (string)parameter };
            }
        }
    }
}
