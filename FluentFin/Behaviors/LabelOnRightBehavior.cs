using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FluentFin.Behaviors
{
	public class LabelOnRightBehavior : Behavior<AppBarButton>
	{
		protected override void OnAttached()
		{
			base.OnAttached();
			var result = VisualStateManager.GoToState(AssociatedObject, "LabelOnRight", false);
		}
	}
}
