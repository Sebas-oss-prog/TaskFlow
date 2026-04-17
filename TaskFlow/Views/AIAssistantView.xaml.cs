using System.Collections.Specialized;
using System.Windows.Controls;
using TaskFlow.ViewModels;

namespace TaskFlow.Views
{
    public partial class AIAssistantView : UserControl
    {
        public AIAssistantView()
        {
            InitializeComponent();
            DataContextChanged += AIAssistantView_DataContextChanged;
        }

        private void AIAssistantView_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is MainViewModel oldVm)
            {
                oldVm.ChatMessages.CollectionChanged -= ChatMessages_CollectionChanged;
            }

            if (e.NewValue is MainViewModel newVm)
            {
                newVm.ChatMessages.CollectionChanged += ChatMessages_CollectionChanged;
            }
        }

        private void ChatMessages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            ChatScrollViewer.ScrollToEnd();
        }
    }
}
