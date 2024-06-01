using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using MessageBox_AvaloniaUI;
using MessageBox_Core;

namespace TerminalProgram.Views
{
    public partial class ServiceWindow : Window
    {
        public string? SelectedFilePath { get; private set; }

        private enum ControlForSelect
        {
            TextBox,
            ComboBox
        }

        private readonly ControlForSelect _controlForSelect;

        private readonly IMessageBox Message;


        public ServiceWindow()
        {
            InitializeComponent();

            Message = new MessageBox(this, "������������ ���������");

            _controlForSelect = ControlForSelect.TextBox;

            TextBlock_Description.Text = "������� ��� �����";

            ComboBox_SelectFileName.IsVisible = false;
            TextBox_SelectFileName.IsVisible = true;
        }

        public ServiceWindow(string[] ArrayOfDocuments)
        {
            InitializeComponent();

            Message = new MessageBox(this, "������������ ���������");

            _controlForSelect = ControlForSelect.ComboBox;

            TextBlock_Description.Text = "�������� ���� ��������";

            ComboBox_SelectFileName.IsVisible = true;
            TextBox_SelectFileName.IsVisible = false;
        }

        private void Chrome_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            this.BeginMoveDrag(e);
        }

        private void Button_Close_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Select_Click(object? sender, RoutedEventArgs e)
        {
            switch (_controlForSelect)
            {
                case ControlForSelect.TextBox:
                    TextBox_ResultHandler();
                    break;

                case ControlForSelect.ComboBox:
                    ComboBox_ResultHandler();
                    break;
            }

            this.Close();
        }

        private void TextBox_ResultHandler()
        {
            SelectedFilePath = TextBox_SelectFileName.Text;
        }

        private void ComboBox_ResultHandler()
        {
            string? SelectedFile = ComboBox_SelectFileName.SelectedItem?.ToString();

            if (SelectedFile == null)
            {
                Message.Show("�� ������� ������� ��������.", MessageType.Error);
            }

            else
            {
                SelectedFilePath = SelectedFile;
            }
        }
    }
}
