using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySales.Object
{
    class Helper
    {
        internal static string AutoCountCommaString(IEnumerable fields)
            => AutoCount.Utils.StringHelper.ArrayListToCommaString(new ArrayList(fields as ICollection));

        internal static int ToInteger(string strPort)
        {
            return int.TryParse(strPort, out int port) ? port : default(int);
        }

        internal static DateTime ToDateTime(string datetime)
        {
            return DateTime.ParseExact(datetime, "dd/MM/yyyy HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
        }

        internal static double calcDiscount(double price, double quantity, double disc1, double disc2, double disc3)
        {
            double finalPrice = 1 * price;

            finalPrice = finalPrice - ((finalPrice * disc1) / 100);

            finalPrice = finalPrice - ((finalPrice * disc2) / 100);

            finalPrice = finalPrice - ((finalPrice * disc3) / 100);

            return finalPrice * quantity;
        }
    }
}
