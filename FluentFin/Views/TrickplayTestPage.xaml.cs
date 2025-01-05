using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FluentFin.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TrickplayTestPage : Page
    {
        const int imageHeight = 180;
        const int imageWidth = 320;

        public TrickplayTestPage()
        {
            this.InitializeComponent();
        }

		private void RowIndex_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
    //        try
    //        {
				//Translate.X = -ColumnIndex.Value * imageWidth;
				//Translate.Y = -RowIndex.Value * imageHeight;
				//Image.Clip = new RectangleGeometry { Rect = new Rect(ColumnIndex.Value * imageWidth, RowIndex.Value * imageHeight, imageWidth, imageHeight) };
    //        }
    //        catch { }
		}

		private void ColumnIndex_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
		{
			//try
			//{
			//	Translate.X = -ColumnIndex.Value * imageWidth;
   //             Translate.Y = -RowIndex.Value * imageHeight;
			//	Image.Clip = new RectangleGeometry { Rect = new Rect(ColumnIndex.Value * imageWidth, RowIndex.Value * imageHeight, imageWidth, imageHeight) };
			//}
			//catch { }
		}
    }
}

