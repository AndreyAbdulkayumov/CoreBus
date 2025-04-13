﻿using MessageBox_AvaloniaUI;
using MessageBox_Core;
using Services.Interfaces;
using System;
using System.Threading.Tasks;
using TerminalProgramBase.Views;

namespace TerminalProgramBase.Services
{
    public class MessageBoxAboutApp : IMessageBoxAboutApp
    {
        private readonly IUIService _uiService;

        private string? _appVersion;

        public MessageBoxAboutApp(IUIService uiService)
        {
            _uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));

            _appVersion = _uiService.GetAppVersion()?.ToString();
        }

        public void Show(string message, MessageType type, Exception? error = null)
        {
            var messageBox = new MessageBox(AboutWindow.Instance, _appVersion);

            messageBox.Show(message, type, error);
        }

        public async Task<MessageBoxResult> ShowYesNoDialog(string message, MessageType type, Exception? error = null)
        {
            var messageBox = new MessageBox(AboutWindow.Instance, _appVersion);

            return await messageBox.ShowYesNoDialog(message, type, error);
        }
    }
}
