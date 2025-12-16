using Avalonia.Controls;

namespace CoreBus.Base.Views.ModbusClient.Monitoring;

public partial class MonitoringSettings : UserControl
{
    public MonitoringSettings()
    {
        InitializeComponent();
    }

    private void UppercaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox? textBox = sender as TextBox;

        if (textBox != null)
        {
            textBox.Text = textBox.Text?.ToUpper();
        }
    }
}