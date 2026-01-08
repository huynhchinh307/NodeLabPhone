using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace NodeLabFarm.Controls.ScriptEditor
{
    public partial class ScriptToolbox : UserControl
    {
        public ScriptToolbox()
        {
            InitializeComponent();
        }

        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower().Trim();
            
            // Iterate over all Expanders in the stack panel
            foreach (var child in CategoriesPanel.Children)
            {
                if (child is CardExpander expander && expander.Content is StackPanel panel)
                {
                    bool hasVisibleChildren = false;

                    foreach (var item in panel.Children)
                    {
                        if (item is Wpf.Ui.Controls.Button button) // Explicitly use Wpf.Ui.Controls.Button or System.Windows.Controls.Button depending on usage. The XAML uses ui:Button.
                        {
                            // We need to find the TextBlock inside the button
                            // Simple visual tree walk or just access Content if it was simple text, 
                            // but here it is a StackPanel with TextBlock.
                            
                            var text = GetButtonText(button);
                            if (string.IsNullOrEmpty(searchText) || text.ToLower().Contains(searchText))
                            {
                                button.Visibility = Visibility.Visible;
                                hasVisibleChildren = true;
                            }
                            else
                            {
                                button.Visibility = Visibility.Collapsed;
                            }
                        }
                        else if (item is System.Windows.Controls.TextBlock placeholder)
                        {
                            // Handle placeholders
                            placeholder.Visibility = string.IsNullOrEmpty(searchText) ? Visibility.Visible : Visibility.Collapsed;
                        }
                    }

                    // If searching, always expand valid groups. If clearing, maybe restore? 
                    // Let's just expand if matches found.
                    if (!string.IsNullOrEmpty(searchText))
                    {
                        expander.Visibility = hasVisibleChildren ? Visibility.Visible : Visibility.Collapsed;
                        if (hasVisibleChildren) expander.IsExpanded = true;
                    }
                    else
                    {
                        // Reset visibility
                        expander.Visibility = Visibility.Visible;
                        // Optional: Collapse all or restore default state. 
                        // For now keep current state or maybe expand first two?
                    }
                }
            }
        }

        private string GetButtonText(Wpf.Ui.Controls.Button button)
        {
            if (button.Content is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is System.Windows.Controls.TextBlock tb) return tb.Text;
                }
            }
            return "";
        }
    }
}
