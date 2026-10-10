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
                typeof(DataGridView).GetMethod("ProcessDataGridViewKey", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(grid, new object[] { new KeyEventArgs(Keys.Down) }); Application.DoEvents();
                Check(grid.ContainsFocus && grid.CurrentCell.RowIndex == 1 && grid.SelectedRows.Count == 1,
                    "native grid arrow selects another record without editing; focus=" + grid.ContainsFocus + " row=" + grid.CurrentCell.RowIndex + " selected=" + grid.SelectedRows.Count);
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
            VaultAtomicPersistence(settings);
        }

        private static void VaultAtomicPersistence(SettingsService settings)
        {
            byte[] previous = File.Exists(AppPaths.VaultPath) ? File.ReadAllBytes(AppPaths.VaultPath) : null;
            var originalAttributes = previous == null ? FileAttributes.Normal : File.GetAttributes(AppPaths.VaultPath);
            string pattern = Path.GetFileName(AppPaths.VaultPath) + ".*.tmp";
            var priorStaging = Directory.GetFiles(AppPaths.Root, pattern);
            try {
                if (File.Exists(AppPaths.VaultPath)) { File.SetAttributes(AppPaths.VaultPath, FileAttributes.Normal); File.Delete(AppPaths.VaultPath); }
                var session = VaultService.Create("1234");
                Check(session.CanPersist && File.Exists(AppPaths.VaultPath) && VaultService.Open("1234").CanPersist,
                    "atomic vault create commits a new file that the existing reader opens");
                var entry = VaultEntry.New(); entry.name = "Исходная запись"; entry.secret = "synthetic-vault-value";
                session.Data.entries.Add(entry); VaultService.Save(session);
                byte[] baseline = File.ReadAllBytes(AppPaths.VaultPath);
                var json = new JavaScriptSerializer(); var envelope = json.Deserialize<VaultEnvelope>(File.ReadAllText(AppPaths.VaultPath));
                Check(envelope.version == 1 && envelope.kdf == "PBKDF2-HMAC-SHA256" && envelope.iterations == 120000 && envelope.cipher == "AES-256-CBC" &&
                    envelope.mac == "HMAC-SHA256" && Convert.FromBase64String(envelope.salt).Length == 32 && Convert.FromBase64String(envelope.iv).Length == 16 &&
                    Convert.FromBase64String(envelope.tag).Length == 32 && !File.ReadAllText(AppPaths.VaultPath).Contains(entry.secret),
                    "atomic writes retain the existing encrypted envelope format without plaintext entry content");
                var candidate = new VaultSession(VaultData.Empty(), "1234", true);
                bool denied = false;
                File.SetAttributes(AppPaths.VaultPath, FileAttributes.ReadOnly);
                try { VaultService.Save(candidate); } catch (IOException) { denied = true; } catch (UnauthorizedAccessException) { denied = true; }
                finally { File.SetAttributes(AppPaths.VaultPath, FileAttributes.Normal); }
                Check(denied && File.ReadAllBytes(AppPaths.VaultPath).SequenceEqual(baseline), "a real denied vault replacement preserves the original ciphertext");
                denied = false;
                using (var locked = File.Open(AppPaths.VaultPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    try { VaultService.Save(candidate); } catch (IOException) { denied = true; } catch (UnauthorizedAccessException) { denied = true; }
                    Check(denied, "an actual target lock refuses atomic vault replacement");
                }
                Check(File.ReadAllBytes(AppPaths.VaultPath).SequenceEqual(baseline) && Directory.GetFiles(AppPaths.Root, pattern).OrderBy(p => p).SequenceEqual(priorStaging.OrderBy(p => p)),
                    "failed native replacements preserve the old file and remove their encrypted staging files");
                using (var clipboard = new ClipboardService(settings))
                using (var form = new VaultForm(session, clipboard, settings)) {
                    form.Show(); Application.DoEvents();
                    var grid = (DataGridView)Field(form, "grid");
                    string dataBefore = json.Serialize(session.Data);
                    using (var locked = File.Open(AppPaths.VaultPath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        foreach (string action in new[] { "AddEntry", "EditSelected", "DeleteSelected" }) {
                            grid.CurrentCell = grid.Rows[0].Cells[0]; grid.Rows[0].Selected = true;
                            string warning = VaultWriteAction(form, action, "Отклонённое изменение", true);
                            Check(warning.Contains("Не удалось сохранить") && warning.Contains("окне не изменены") && !warning.Contains(entry.secret),
                                "actual vault " + action + " shows a clear write-error warning without secret contents");
                            Check(json.Serialize(session.Data) == dataBefore && grid.Rows.Count == 1 && ReferenceEquals(grid.Rows[0].Tag, entry) &&
                                Convert.ToString(grid.Rows[0].Cells[0].Value) == entry.name,
                                "failed actual " + action + " creates no phantom session or grid edit");
                        }
                    }
                    Check(File.ReadAllBytes(AppPaths.VaultPath).SequenceEqual(baseline), "failed actual vault actions leave the original encrypted file unchanged");
                    VaultWriteAction(form, "AddEntry", "Сохранённая запись", false);
                    Check(session.Data.entries.Count == 2 && grid.Rows.Count == 2, "unlocked Add publishes its successful committed record");
                    grid.CurrentCell = grid.Rows.Cast<DataGridViewRow>().First(r => ((VaultEntry)r.Tag).id == entry.id).Cells[0];
                    grid.CurrentRow.Selected = true;
                    VaultWriteAction(form, "EditSelected", "Сохранённое изменение", false);
                    Check(session.Data.entries.Single(e => e.id == entry.id).name == "Сохранённое изменение", "unlocked Edit publishes only the committed replacement");
                    grid.CurrentCell = grid.Rows.Cast<DataGridViewRow>().First(r => ((VaultEntry)r.Tag).id == entry.id).Cells[0];
                    grid.CurrentRow.Selected = true;
                    VaultWriteAction(form, "DeleteSelected", null, false);
                    var reopened = VaultService.Open("1234");
                    Check(session.Data.entries.Count == 1 && grid.Rows.Count == 1 && reopened.CanPersist && json.Serialize(reopened.Data) == json.Serialize(session.Data),
                        "unlocked Delete and all committed UI edits reopen through the unchanged vault reader");
                    form.Close();
                }
                baseline = File.ReadAllBytes(AppPaths.VaultPath);
                var temporary = new VaultSession(VaultData.Empty(), "1234", false);
                using (var clipboard = new ClipboardService(settings))
                using (var form = new VaultForm(temporary, clipboard, settings)) {
                    form.Show(); Application.DoEvents(); VaultWriteAction(form, "AddEntry", "Запись текущего сеанса", false);
                    Check(temporary.Data.entries.Count == 1 && ((DataGridView)Field(form, "grid")).Rows.Count == 1 &&
                        File.ReadAllBytes(AppPaths.VaultPath).SequenceEqual(baseline), "nonpersistent session keeps its existing in-memory UI behavior and leaves the real file untouched");
                    form.Close();
                }
                Check(Directory.GetFiles(AppPaths.Root, pattern).OrderBy(p => p).SequenceEqual(priorStaging.OrderBy(p => p)), "successful vault commits leave no new encrypted staging files");
            } finally {
                if (File.Exists(AppPaths.VaultPath)) File.SetAttributes(AppPaths.VaultPath, FileAttributes.Normal);
                if (previous == null) { if (File.Exists(AppPaths.VaultPath)) File.Delete(AppPaths.VaultPath); }
                else { File.WriteAllBytes(AppPaths.VaultPath, previous); File.SetAttributes(AppPaths.VaultPath, originalAttributes); }
            }
        }
        private static string VaultWriteAction(VaultForm form, string action, string name, bool expectWarning)
        {
            bool editorSaved = false, confirmed = false, warned = false; var text = new System.Text.StringBuilder();
            using (var timer = new Timer { Interval = 30 }) {
                timer.Tick += delegate {
                    var warning = FindWindow("#32770", "Сохранение хранилища");
                    if (warning != IntPtr.Zero) {
                        warned = true;
                        EnumChildWindows(warning, delegate(IntPtr child, IntPtr data) {
                            var value = new System.Text.StringBuilder(2048); GetWindowText(child, value, value.Capacity); text.AppendLine(value.ToString()); return true;
                        }, IntPtr.Zero);
                        PostMessage(warning, 0x0010, IntPtr.Zero, IntPtr.Zero); return;
                    }
                    if (action == "DeleteSelected") {
                        var dialog = FindWindow("#32770", "Удаление записи");
                        if (dialog != IntPtr.Zero && !confirmed) { confirmed = true; PostMessage(dialog, 0x0111, (IntPtr)1, IntPtr.Zero); }
                        return;
                    }
                    var editor = Application.OpenForms.OfType<EntryForm>().FirstOrDefault();
                    if (editor == null || editorSaved) return;
                    editorSaved = true; ((TextBox)Field(editor, "name")).Text = name;
                    ((TextBox)Field(editor, "secret")).Text = "synthetic-vault-ui-value";
                    ((Button)editor.AcceptButton).PerformClick();
                };
                timer.Start(); Call(form, action);
            }
            Check(warned == expectWarning && (action == "DeleteSelected" ? confirmed : editorSaved), "native vault action reaches its expected save outcome: " + action);
            return text.ToString();
        }
    }
}
