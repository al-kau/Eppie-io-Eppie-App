using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;
using System.Windows.Input;

#if WINDOWS_UWP
using Windows.UI.Xaml;
#else
using Microsoft.UI.Xaml;
#endif

namespace Eppie.App.UI.Behaviors
{
    public class TreeViewItemInvokeBehavior : Behavior<TreeView>
    {
        public ICommand InvokeCommand
        {
            get { return (ICommand)GetValue(InvokeCommandProperty); }
            set { SetValue(InvokeCommandProperty, value); }
        }

        public static readonly DependencyProperty InvokeCommandProperty =
            DependencyProperty.Register(nameof(InvokeCommand), typeof(ICommand), typeof(TreeViewItemInvokeBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            if (AssociatedObject != null)
            {
                //TODO: TVM-283 Remove when Microsoft fixes bug: https://github.com/microsoft/microsoft-ui-xaml/issues/4999
                AssociatedObject.ItemInvoked -= OnItemInvoked;
                // End
                AssociatedObject.ItemInvoked += OnItemInvoked;
            }
        }

        protected override void OnDetaching()
        {
            if (AssociatedObject != null)
            {
                //TODO: TVM-283 Remove when Microsoft fixes bug: https://github.com/microsoft/microsoft-ui-xaml/issues/4999
                if (AssociatedObject.Parent == null)
                // End
                {
                    AssociatedObject.ItemInvoked -= OnItemInvoked;
                }
            }
        }

        private void OnItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            InvokeCommand?.Execute(args?.InvokedItem);
        }
    }
}
