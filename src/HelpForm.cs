using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ProGo
{
    internal sealed class HelpForm : ProGoForm
    {
        internal HelpForm(Action openLog)
        {
            Text = "Помощь · ProGo"; ClientSize = new Size(780, 570); MinimumSize = new Size(700, 540);
            var body = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28), AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            body.Controls.Add(UiTheme.Label("Антивирус и обновления", UiTheme.Title, UiTheme.Text));
            Paragraph(body, "ProGo не отключает защиту Windows и не создаёт исключения в антивирусе. Если файл заблокирован, сначала нужно выяснить причину обнаружения.");
            Paragraph(body, "1. Откройте отчёт Касперского. Сохраните название обнаружения и имя заблокированного файла. Пароли, токены и содержимое хранилища отправлять не нужно.");
            Paragraph(body, "2. Сверьте версию с официальным выпуском. Обновления 0.2 проверяют SHA-256 архива по данным GitHub и запускают установленный файл обновления. Код с сервера не выполняется в памяти.");
            Paragraph(body, "3. Если официальный файл продолжает блокироваться, запросите проверку ложного срабатывания через Kaspersky OpenTIP. Само наличие блокировки ещё не доказывает, что она ложная.");
            Paragraph(body, "Исполняемый файл пока не имеет подписи издателя Authenticode. Проверка SHA-256 контролирует целостность пакета, но не заменяет подпись и не гарантирует доверие антивируса.");
            var actions = new FlowLayoutPanel { Width = 704, Height = 105, WrapContents = true };
            actions.Controls.Add(UiTheme.Button("Официальный выпуск", delegate { Open("https://github.com/rkhnorkhan-bit/ProGo/releases/latest"); }, true));
            actions.Controls.Add(UiTheme.Button("Журнал обновления", delegate { openLog(); }, false));
            actions.Controls.Add(UiTheme.Button("Kaspersky OpenTIP", delegate { Open("https://opentip.kaspersky.com/"); }, false));
            body.Controls.Add(actions); Controls.Add(body);
        }
        private static void Paragraph(Control body, string text)
        {
            var label = UiTheme.Label(text, UiTheme.Body, UiTheme.Muted); label.MaximumSize = new Size(684, 0); label.Margin = new Padding(0, 8, 0, 14); body.Controls.Add(label);
        }
        private static void Open(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Не удалось открыть браузер: " + ex.Message, "ProGo"); }
        }
    }
}
