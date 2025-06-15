using Avalonia.Controls;

namespace CoreBus.Base.Views.NoProtocol;

public partial class NoProtocol_View : UserControl
{
    public NoProtocol_View()
    {
        InitializeComponent();
    }

    private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        var element = sender as TextBox;

        // ��������� ������ � �����

        if (element != null)
        {
            // ������ ��� CaretIndex ��������� ������ �� ��������� ��������,
            // ������� ����� Text ��������� ������������� ���������� ��������
            // ��������� �� ������ ����������.
            element.CaretIndex = 0;
            element.CaretIndex = element.Text == null ? 0 : element.Text.Length;
        }
    }
}
