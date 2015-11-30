using Smithers.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Monocle
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    }

    [ValueConversion(typeof(bool), typeof(bool))]
    public class InverseBooleanConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }

    [ValueConversion(typeof(UploadResult), typeof(string))]
    public class UploadResultToMessageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "Uploading...";

            UploadResult result = value as UploadResult;
            string message = "";

            if (result.Success)
            {
                List<Uploader.DS4Artifacts> artifacts = result.Artifacts;

                foreach (var item in artifacts)
                {
                    message += "Status: Compeleted" + Environment.NewLine;
                    message += "Type: " + item.ArtifactsType + Environment.NewLine;
                    message += "Id: " + item.ArtifactsId + Environment.NewLine;
                    message += Environment.NewLine;
                }

                return message;

            }

            if (result.Canceled) return "UPLOAD CANCELED";

            message = Uploader.GetErrorMsgFromWebException(result.Exception);
            
            return message == "" ? result.Exception.Message : "UPLOAD FAILED: " + Environment.NewLine + message;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class InverseBooleanVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Visibility))
                throw new InvalidOperationException("The target must be a boolean");

            if (!(bool)value)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
