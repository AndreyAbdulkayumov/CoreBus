using Avalonia.Controls;

namespace TerminalProgram.Views.Protocols
{
    public partial class NoProtocol : UserControl
    {
        public NoProtocol()
        {
            InitializeComponent();

            ScrollViewer? e2 = ControlExtensions.FindControl<ScrollViewer>(TextBox_Test, "PART_ScrollViewer");
        }
                
        private void TextBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            TextBox? element = sender as TextBox;

            // ��������� ������ � �����

            if (element != null)
            {
                // ������ ��� CaretIndex ��������� ������ �� ��������� ��������,
                // ������� ����� Text ��������� ������������� ���������� ��������
                // ��������� �� ������ ����������.
                element.CaretIndex = 0;
                element.CaretIndex = element.Text == null ? 0 : element.Text.Length;

                //ScrollViewer_RX.ScrollToEnd();
            }
        }
    }
}
