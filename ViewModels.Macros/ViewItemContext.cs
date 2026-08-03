using Core.Models.Settings.DataTypes;
using ReactiveUI.Reactive;
using ViewModels.Macros.DataTypes;

namespace ViewModels.Macros;

public class ViewItemContext<T1, T2> : IMacrosContext
{
    private readonly MacrosContent<T1, T2> _content;
    private readonly string? _senderName;

    public ViewItemContext(MacrosContent<T1, T2> content, string? senderName)
    {
        _content = content;
        _senderName = senderName;
    }

    public MacrosViewItemData CreateContext()
    {
        var contentForSend = MacrosHelper.GetWithAdditionalData(_content);
        contentForSend.Sender = _senderName;

        Action action = () =>
        {
            if (contentForSend.Commands == null)
            {
                return;
            }

            MessageBus.Current.SendMessage(contentForSend);
        };

        return new MacrosViewItemData(contentForSend.MacrosName ?? string.Empty, action);
    }
}
