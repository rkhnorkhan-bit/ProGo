using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace ProGo
{
    internal static partial class DesktopTests
    {
        private static void VaultAccessibility(SettingsService settings)
        {
            if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true") {
                Console.WriteLine("SKIP: vault keyboard fixtures require isolated native CI"); return;
            }
            var vaultBefore = File.Exists(AppPaths.VaultPath) ? File.ReadAllBytes(AppPaths.VaultPath) : null;
            var data = VaultData.Empty();
            var first = VaultEntry.New(); first.name = "Пример пароля"; first.secret = "synthetic-private-value";
            first.login = "example-user"; first.url_or_host = "example.org";
            var second = VaultEntry.New(); second.name = "Пример токена"; second.type = "token"; second.secret = "synthetic-token-value";
            data.entries.Add(first); data.entries.Add(second);
            var serialized = new JavaScriptSerializer().Serialize(data);
            using (var clipboard = new ClipboardService(settings))
            using (var form = new VaultForm(new VaultSession(data, "1234", false), clipboard, settings)) {
                form.Show(); Application.DoEvents();
                var search = (TextBox)Field(form, "search"); var filter = (ComboBox)Field(form, "typeFilter");
                var grid = (DataGridView)Field(form, "grid");
                Func<string, Button> button = text => Descendants(form).OfType<Button>().Single(b => b.Text == text);
                var actions = new[] { button("Добавить"), button("Изменить"), button("Удалить"), button("Копировать секрет"), button("Заблокировать"), button("Закрыть") };
                var order = new Control[] { search, filter, grid }.Concat(actions).ToArray();
                Check(search.AccessibilityObject.Name == "Поиск записей" && filter.AccessibilityObject.Name == "Фильтр по типу записи" &&
                    grid.AccessibilityObject.Name == "Записи хранилища", "vault filters and read-only list expose their purpose");
                Check(grid.ReadOnly && grid.StandardTab && form.AcceptButton == null && form.CancelButton == actions.Last(),
                    "vault list uses arrows within rows and Tab for actions without an implicit destructive default");
                Check(actions.All(b => !String.IsNullOrEmpty(b.AccessibilityObject.Description)) &&
                    actions[2].AccessibilityObject.Description.Contains("подтверждения") && actions[3].AccessibilityObject.Description.Contains(clipboard.CopyNotice),
                    "vault actions describe confirmation, saving and current clipboard cleanup policy");
                Check(grid.Rows.Count == 2 && grid.Rows.Cast<DataGridViewRow>().All(r => r.Cells.Cast<DataGridViewCell>().All(c =>
                    Convert.ToString(c.Value) != first.secret && Convert.ToString(c.Value) != second.secret)), "vault list never includes secret contents");
                KeyboardWalk(form, order, "vault populated list");
                grid.Focus(); grid.CurrentCell = grid.Rows[0].Cells[0];
                var gridKey = typeof(DataGridView).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic);
                gridKey.Invoke(grid, new object[] { Keys.Down }); Application.DoEvents();
                Check(grid.ContainsFocus && grid.CurrentCell.RowIndex == 1 && grid.SelectedRows.Count == 1, "native grid arrow selects another record without editing");
                gridKey.Invoke(grid, new object[] { Keys.Tab }); Application.DoEvents();
                Check(actions[0].ContainsFocus, "native grid Tab leaves the cells for Add");
                grid.Focus(); gridKey.Invoke(grid, new object[] { Keys.Tab | Keys.Shift }); Application.DoEvents();
                Check(filter.ContainsFocus, "native grid Shift+Tab returns to type filter");
                filter.SelectedItem = "Токен"; Application.DoEvents();
                Check(grid.Rows.Count == 1 && ReferenceEquals(grid.Rows[0].Tag, second), "accessible type filter retains existing filtering semantics");
                search.Text = "no-such-record"; Application.DoEvents();
                Check(grid.Rows.Count == 0, "accessible search retains empty-result behavior");
                KeyboardWalk(form, order, "vault empty search results");
                search.Text = ""; filter.SelectedIndex = 0; Application.DoEvents();
                Shot(form, "keyboard-vault");
                var formKey = typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic);
                search.Focus(); formKey.Invoke(form, new object[] { Keys.Escape }); Application.DoEvents();
                Check(form.DialogResult == DialogResult.Cancel && new JavaScriptSerializer().Serialize(data) == serialized,
                    "vault Escape and keyboard traversal leave records unchanged");
            }
            using (var form = new EntryForm(first.Clone())) {
                form.Show(); Application.DoEvents();
                var fields = new[] { "name", "type", "login", "secret", "host", "tags", "notes" }.Select(f => (Control)Field(form, f)).ToArray();
                var labels = new[] { "Название", "Тип", "Логин", "Секрет", "Сайт / сервер", "Метки", "Заметки" };
                Check(fields.Select(c => c.AccessibilityObject.Name).SequenceEqual(labels), "entry editor fields match their visible labels");
                var secret = (TextBox)Field(form, "secret"); var show = Descendants(form).OfType<CheckBox>().Single();
                var save = (Button)form.AcceptButton; var cancel = (Button)form.CancelButton;
                Check(secret.UseSystemPasswordChar && secret.AccessibilityObject.Description.Contains("скрыто") && !show.Checked,
                    "entry editor starts with a protected secret and a safe explanation");
                KeyboardWalk(form, fields.Concat(new Control[] { show, save, cancel }).ToArray(), "entry editor");
                show.Checked = true;
                Check(!secret.UseSystemPasswordChar && secret.AccessibilityObject.Description.Contains("отображается") && form.Entry.secret == first.secret,
                    "show-secret changes presentation without committing a value");
                show.Checked = false;
                Check(secret.UseSystemPasswordChar && secret.AccessibilityObject.Description.Contains("скрыто"), "hide-secret restores masking and its explanation");
                ((TextBox)fields[0]).Text = "Несохранённое название"; secret.Text = "unsaved-synthetic-value";
                Check(save.AccessibilityObject.Description.Contains("сохраняет") && cancel.AccessibilityObject.Description.Contains("без сохранения"),
                    "entry save and cancel explain their different effects");
                Shot(form, "keyboard-entry");
                typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape }); Application.DoEvents();
                Check(form.DialogResult == DialogResult.Cancel && form.Entry.name == first.name && form.Entry.secret == first.secret,
                    "entry Escape discards pending editor fields");
            }
            using (var form = new EntryForm(first.Clone())) {
                form.Show(); Application.DoEvents();
                ((TextBox)Field(form, "name")).Text = "Сохранённый пример";
                ((Button)form.AcceptButton).PerformClick(); Application.DoEvents();
                Check(form.DialogResult == DialogResult.OK && form.Entry.name == "Сохранённый пример" && first.name == "Пример пароля",
                    "accessible entry Save retains ordinary clone-edit commit behavior"); form.Close();
            }
            foreach (bool create in new[] { true, false }) {
                using (var form = new PinForm(create)) {
                    form.Show(); Application.DoEvents();
                    var pin = (TextBox)Field(form, "pin"); var confirm = (TextBox)Field(form, "confirm");
                    var ok = (Button)form.AcceptButton; var cancel = (Button)form.CancelButton;
                    Check(pin.AccessibilityObject.Name == "PIN-код" && pin.UseSystemPasswordChar &&
                        pin.AccessibilityObject.Description.Contains("четырёх цифр"), "PIN field is named and masked for create=" + create);
                    if (create) Check(confirm.AccessibilityObject.Name == "Повторите PIN-код" && confirm.UseSystemPasswordChar,
                        "PIN confirmation has a distinct name and masked input");
                    KeyboardWalk(form, create ? new Control[] { pin, confirm, ok, cancel } : new Control[] { pin, ok, cancel }, "PIN create=" + create);
                    Check(!String.IsNullOrEmpty(ok.AccessibilityObject.Description) && cancel.AccessibilityObject.Description.Contains("без создания"),
                        "PIN actions explain confirmation and cancellation for create=" + create);
                    pin.Text = "1234"; if (create) confirm.Text = "1234";
                    Shot(form, create ? "keyboard-pin-create" : "keyboard-pin-unlock");
                    typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(form, new object[] { Keys.Escape }); Application.DoEvents();
                    Check(form.DialogResult == DialogResult.Cancel, "PIN Escape keeps create/unlock cancelled for create=" + create);
                }
            }
            Check(new JavaScriptSerializer().Serialize(data) == serialized, "vault accessibility work never mutates fixture records");
            Check(vaultBefore == null ? !File.Exists(AppPaths.VaultPath) : File.Exists(AppPaths.VaultPath) && File.ReadAllBytes(AppPaths.VaultPath).SequenceEqual(vaultBefore),
                "vault accessibility fixtures never create, persist or open an encrypted vault");
        }
    }
}
