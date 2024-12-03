using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FluentFin.Views
{
	public sealed partial class TitleBarControl : UserControl
	{
		public TitleBarControl()
		{
			this.InitializeComponent();
		}

		private void TitleBar_PaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
		{
			if(DataContext is not ITitleBarViewModel vm)
			{
				return;
			}

			vm.TogglePane();
        }

		private void TitleBar_BackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
		{
			if (DataContext is not ITitleBarViewModel vm)
			{
				return;
			}

			vm.GoBack();
		}
    }
}
