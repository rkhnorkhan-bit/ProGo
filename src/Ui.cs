using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using CancellationToken = System.Threading.CancellationToken;
using CancellationTokenSource = System.Threading.CancellationTokenSource;

namespace ProGo
{
    internal sealed class StatusForm : ProGoForm
    {
        private readonly SettingsService settings;
        private readonly ProxyService proxy;
        private readonly ConnectionHealthMonitor health;
        private readonly Label state;
        private readonly Label address;
        private readonly Label pid;
        private readonly Label env;
        private readonly Label ping;
        private readonly Label speed;
        private readonly Label route;
        private readonly Label checkedAt;
        private readonly Label recovery;
        private readonly Button speedButton;
        private readonly Button checkButton;
        private readonly Func<AppSettings, ProxyService, CancellationToken, string> routeProbe;
        private readonly Func<AppSettings, CancellationToken, int?> pingProbe;
        private readonly Func<AppSettings, CancellationToken, Tuple<double?, string>> speedProbe;
        private CancellationTokenSource routeCancellation, pingCancellation, speedCancellation;
        internal Task RouteWork { get; private set; }
        internal Task PingWork { get; private set; }
        internal Task SpeedWork { get; private set; }
        private readonly Func<DateTime> now;
        private readonly Timer pingTimer;
        private readonly Control measurementDispatcher = new Control();
        private readonly object measurementCompletionGate = new object();
        private readonly HashSet<TaskCompletionSource<bool>> measurementCompletions = new HashSet<TaskCompletionSource<bool>>();
        private int pingInFlight;
        private int speedInFlight;
        private int routeInFlight;
        private volatile bool closing;

        public StatusForm(SettingsService settingsService, ProxyService proxyService, bool checkRouteOnOpen = false,
            Func<AppSettings, ProxyService, CancellationToken, string> routeProbe = null, Func<DateTime> clock = null, ConnectionHealthMonitor health = null,
            Func<AppSettings, CancellationToken, int?> pingProbe = null,
            Func<AppSettings, CancellationToken, Tuple<double?, string>> speedProbe = null)
        {
            settings = settingsService;
            proxy = proxyService;
            this.health = health;
            this.routeProbe = routeProbe ?? ((s, p, token) => RouteTester.Test(s, p, token));
            this.pingProbe = pingProbe ?? ((s, token) => ConnectionMetrics.MeasureSocksLatencyMs(s, 4000, token));
            this.speedProbe = speedProbe ?? ((s, token) => { string error; var value = ConnectionMetrics.MeasureDownloadMbps(s, token, out error); return Tuple.Create(value, error); });
            now = clock ?? (() => DateTime.Now);
            Text = "Маршрут и скорость · ProGo";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(820, 570);
            MinimumSize = new Size(790, 610);

            // Keep the footer outside the two-column table: its preferred width must
            // not expand a spanning cell past the form's real minimum client width.
            var content = new Panel { Dock = DockStyle.Fill, Padding = UiTheme.DensePadding };
            Controls.Add(content);
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            content.Controls.Add(table);

            state = AddRow(table, 0, "Соединение");
            address = AddRow(table, 1, "Адрес");
            pid = AddRow(table, 2, "SSH-процесс");
            env = AddRow(table, 3, "Командная строка");
            ping = AddRow(table, 4, "Задержка соединения");
            speed = AddRow(table, 5, "Скорость");
            route = AddRow(table, 6, "Маршрут");
            checkedAt = AddRow(table, 7, "Проверка маршрута");
            recovery = AddRow(table, 8, "Автовосстановление");
            table.RowStyles[6].Height = 82;
            table.RowStyles[7].Height = 60;
            table.RowStyles[8].Height = 68;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom,
                Height = UiTheme.ActionHeight + UiTheme.ActionMargin.Vertical,
                FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            var close = UiTheme.Button("Закрыть", null, false, DialogResult.Cancel);
            var restart = UiTheme.Button("Переподключиться", null, false);
            speedButton = UiTheme.Button("Измерить скорость", null, false);
            checkButton = UiTheme.Button("Проверить маршрут", null, false);
            restart.Click += async delegate
            {
                if (health != null) health.Invalidate();
                restart.Enabled = false; bool ready = false, cancelled = false;
                try { ready = await proxy.RestartTunnelAsync(System.Threading.CancellationToken.None).ConfigureAwait(false); }
                catch (OperationCanceledException) { cancelled = true; }
                if (closing || IsDisposed) return;
                try { BeginInvoke(new Action(delegate {
                    if (closing || IsDisposed) return;
                    try { if (!ready && !cancelled) MessageBox.Show(this, proxy.StartupError, "Подключение ProGo", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                    finally { if (health != null) health.Invalidate(); restart.Enabled = true; }
                    RefreshState(false); QueuePingMeasure();
                })); } catch (InvalidOperationException) { }
            };
            speedButton.Click += delegate { if (speedInFlight != 0) CancelMeasurement(speedCancellation); else StartSpeedTest(); };
            checkButton.Click += delegate
            {
                if (routeInFlight != 0) { CancelMeasurement(routeCancellation); CancelMeasurement(pingCancellation); }
                else { QueueRouteMeasure(); QueuePingMeasure(); }
            };
            buttons.Controls.Add(close);
            buttons.Controls.Add(restart);
            buttons.Controls.Add(speedButton);
            buttons.Controls.Add(checkButton);
            content.Controls.Add(buttons);
            close.AccessibleDescription = "Закрывает диагностику и отменяет её измерения. Не отключает подключение ProGo.";
            restart.AccessibleDescription = "Останавливает текущий SSH-туннель ПК и повторно подключается по сохранённым настройкам.";
            DescribeMeasurementButton(checkButton, "Проверить маршрут",
                "Проверяет доступ к сайту через SOCKS и измеряет задержку. Настройки подключения не изменяются.",
                "Отменяет проверку маршрута и текущий замер задержки. Измерение скорости управляется отдельно.");
            DescribeMeasurementButton(speedButton, "Измерить скорость",
                "Загружает тестовые данные через SOCKS для измерения скорости. Использует интернет-трафик.",
                "Отменяет текущий замер скорости. Проверка маршрута управляется отдельно.");
            UiTheme.ConfigureKeyboardOrder(this);
            CancelButton = close;

            pingTimer = new Timer { Interval = 2000 };
            pingTimer.Tick += delegate { QueuePingMeasure(); };
            FormClosed += delegate
            {
                CloseMeasurements(); pingTimer.Stop();
            };

            // A modal window can replace/remove SynchronizationContext. This handle always
            // belongs to the constructor's UI thread, even before the form itself is shown.
            measurementDispatcher.CreateControl();
            RefreshState(false);
            route.Text = "Маршрут ещё не проверен";
            checkedAt.Text = "Ещё не проверен";
            ping.Text = "Измерение...";
            speed.Text = "—";
            Shown += delegate { QueuePingMeasure(); pingTimer.Start(); if (checkRouteOnOpen) QueueRouteMeasure(); };
        }

        private static void DescribeMeasurementButton(Button button, string idleText, string idleEffect, string cancelEffect)
        {
            Action refresh = delegate {
                button.AccessibleName = button.Text;
                button.AccessibleDescription = button.Text == idleText ? idleEffect : cancelEffect;
            };
            button.TextChanged += delegate { refresh(); };
            refresh();
        }

        private static Label AddRow(TableLayoutPanel table, int row, string name)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            table.Controls.Add(new Label { Text = name, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            var value = new Label { Text = "—", AutoSize = true, Anchor = AnchorStyles.Left, MaximumSize = new Size(400, 0) };
            value.AccessibleName = name;
            value.AccessibleDescription = value.Text;
            value.TextChanged += delegate { value.AccessibleDescription = value.Text; };
            table.Controls.Add(value, 1, row);
            return value;
        }

        private void RefreshState(bool testRoute)
        {
            state.Text = (health == null ? "Подключение не проверено" : health.Current.Title) + " · статус " + now().ToString("HH:mm:ss");
            address.Text = settings.Current.SocksHost + ":" + settings.Current.SocksPort;
            pid.Text = proxy.CurrentPid.HasValue ? "PID " + proxy.CurrentPid.Value : "Не запущен этим экземпляром";
            env.Text = Environment.GetEnvironmentVariable("ALL_PROXY", EnvironmentVariableTarget.User) ?? "Не настроен";
            if (testRoute) QueueRouteMeasure();
            recovery.Text = proxy.RecoveryStatus;
        }

        private static void CancelMeasurement(CancellationTokenSource source)
        { if (source != null) try { source.Cancel(); } catch (ObjectDisposedException) { } }
        private void CancelMeasurements()
        { CancelMeasurement(routeCancellation); CancelMeasurement(pingCancellation); CancelMeasurement(speedCancellation); }

        private void CloseMeasurements()
        {
            closing = true; CancelMeasurements();
            lock (measurementCompletionGate) {
                // A close may discard queued native callbacks. Settle their tasks without
                // waiting for the message loop or publishing into the closed form.
                foreach (var completion in measurementCompletions) completion.TrySetResult(false);
                measurementCompletions.Clear();
            }
        }
        private Task<bool> DispatchMeasurementCompletion(Action action)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (measurementCompletionGate) {
                if (closing) return Task.FromResult(false);
                measurementCompletions.Add(completion);
            }
            try {
                measurementDispatcher.BeginInvoke(new Action(delegate {
                    try {
                        if (closing || IsDisposed) { completion.TrySetResult(false); return; }
                        action(); completion.TrySetResult(true);
                    } catch (Exception ex) { completion.TrySetException(ex); }
                    finally { lock (measurementCompletionGate) measurementCompletions.Remove(completion); }
                }));
            } catch (InvalidOperationException) {
                lock (measurementCompletionGate) measurementCompletions.Remove(completion);
                completion.TrySetResult(false);
            }
            return completion.Task;
        }

        // The worker disposes its source even when the application message loop has ended.
        // Completion/cancellation cannot publish into a closed window or replace a newer run.
        private async Task RunMeasurement<T>(CancellationTokenSource source, Func<CancellationToken, T> measure,
            Action<T> apply, Action cancelled, Action failed, Action release, Action finishUi = null)
        {
            var token = source.Token; T result = default(T); bool wasCancelled = false; Exception failure = null;
            int released = 0;
            Action releaseOnce = delegate { if (System.Threading.Interlocked.Exchange(ref released, 1) == 0) release(); };
            try {
                result = await Task.Run(() => { try { return measure(token); } finally { source.Dispose(); } }).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { wasCancelled = true; }
            catch (Exception ex) { failure = ex; SafeLog.Error("Diagnostic measurement failed.", ex); }
            try {
                await DispatchMeasurementCompletion(delegate {
                    try {
                        if (wasCancelled || token.IsCancellationRequested) cancelled();
                        else if (failure != null) failed();
                        else apply(result);
                    } catch (Exception ex) {
                        SafeLog.Error("Diagnostic result publication failed.", ex);
                        if (!closing && !IsDisposed) failed();
                    } finally {
                        releaseOnce();
                        if (!closing && !IsDisposed && finishUi != null) finishUi();
                    }
                }).ConfigureAwait(false);
            } finally { releaseOnce(); }
        }
        private void QueueRouteMeasure()
        {
            if (closing || System.Threading.Interlocked.Exchange(ref routeInFlight, 1) != 0) return;
            var current = settings.Current.Clone(); var source = new CancellationTokenSource(); routeCancellation = source;
            checkButton.Text = "Отменить проверку";
            route.Text = "Проверяем маршрут через SOCKS… Лимит — 10 секунд.";
            RouteWork = RunMeasurement(source, token => routeProbe(current, proxy, token), result => {
                route.Text = result;
                checkedAt.Text = now().ToString("yyyy-MM-dd HH:mm:ss") + "\nSOCKS → " + SafeLog.Redact(current.TestEndpoint);
            }, () => route.Text = "Проверка маршрута отменена.", () => route.Text = "Не удалось проверить маршрут через SOCKS.", () => {
                routeCancellation = null; System.Threading.Interlocked.Exchange(ref routeInFlight, 0);
            }, () => checkButton.Text = "Проверить маршрут");
        }
        private void QueuePingMeasure()
        {
            if (closing || System.Threading.Interlocked.Exchange(ref pingInFlight, 1) != 0) return;
            var current = settings.Current.Clone(); var source = new CancellationTokenSource(); pingCancellation = source;
            PingWork = RunMeasurement(source, token => pingProbe(current, token), latency => {
                ping.Text = (latency.HasValue ? latency.Value + " ms" : "Нет ответа") + "\nSOCKS · " + now().ToString("HH:mm:ss");
                RefreshState(false);
            }, () => ping.Text = "Измерение задержки отменено.", () => ping.Text = "Не удалось измерить задержку.", () => {
                pingCancellation = null; System.Threading.Interlocked.Exchange(ref pingInFlight, 0);
            });
        }
        private void StartSpeedTest()
        {
            if (closing || System.Threading.Interlocked.Exchange(ref speedInFlight, 1) != 0) return;
            var current = settings.Current.Clone(); var source = new CancellationTokenSource(); speedCancellation = source;
            speedButton.Text = "Отменить замер"; speed.Text = "Измеряем через SOCKS… Лимит — 40 секунд.";
            SpeedWork = RunMeasurement(source, token => speedProbe(current, token), result => {
                if (result.Item1.HasValue) speed.Text = result.Item1.Value.ToString("0.0") + " Мбит/с ↓";
                else {
                    speed.Text = "Не удалось измерить";
                    if (!String.IsNullOrWhiteSpace(result.Item2)) SafeLog.Error("Speed test failed: " + result.Item2 + ".", new InvalidOperationException(result.Item2));
                }
                speed.Text += "\nCloudflare через SOCKS · " + now().ToString("HH:mm:ss");
            }, () => speed.Text = "Измерение скорости отменено.", () => speed.Text = "Не удалось измерить скорость.", () => {
                speedCancellation = null; System.Threading.Interlocked.Exchange(ref speedInFlight, 0);
            }, () => speedButton.Text = "Измерить скорость");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CloseMeasurements();
                measurementDispatcher.Dispose();
                if (pingTimer != null)
                {
                    pingTimer.Stop();
                    pingTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class VaultForm : ProGoForm
    {
        private readonly VaultSession session;
        private readonly ClipboardService clipboard;
        private readonly SettingsService settings;
        private readonly TextBox search = new TextBox();
        private readonly ComboBox typeFilter = new ComboBox();
        private readonly DataGridView grid = new DataGridView();
        private readonly Label emptyState = UiTheme.Label("", UiTheme.Body, UiTheme.Muted);
        private const string GridDescription = "Список записей без содержимого секретов. Стрелки выбирают запись; Tab переходит к действиям. Для редактирования используйте «Изменить».";

        public VaultForm(VaultSession vaultSession, ClipboardService clipboardService, SettingsService settingsService)
        {
            session = vaultSession;
            clipboard = clipboardService;
            settings = settingsService;
            Text = "Хранилище секретов";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1040, 600);
            MinimumSize = new Size(980, 560);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = UiTheme.DensePadding, RowCount = 3, ColumnCount = 1 };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, UiTheme.ActionHeight + UiTheme.ActionMargin.Vertical));
            Controls.Add(root);

            var filters = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            filters.Controls.Add(new Label { Text = "Поиск", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
            search.Width = 300;
            search.AccessibleName = "Поиск записей";
            search.AccessibleDescription = "Фильтрует записи по названию, логину, сайту, меткам и заметкам. Содержимое секретов не используется.";
            search.TextChanged += delegate { Reload(); };
            filters.Controls.Add(search);
            filters.Controls.Add(new Label { Text = "Фильтр по типу", AutoSize = true, Padding = new Padding(12, 7, 0, 0) });
            typeFilter.AccessibleName = "Фильтр по типу записи";
            typeFilter.AccessibleDescription = "Показывает записи выбранного типа; «Все» снимает фильтр по типу.";
            typeFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            typeFilter.Items.AddRange(new object[] { "Все", "API-ключ", "Пароль", "Токен", "SSH", "Заметка", "Другое" });
            typeFilter.SelectedIndex = 0;
            typeFilter.SelectedIndexChanged += delegate { Reload(); };
            filters.Controls.Add(typeFilter);
            root.Controls.Add(filters, 0, 0);

            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.StandardTab = true;
            grid.AccessibleName = "Записи хранилища";
            grid.AccessibleDescription = GridDescription;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.DoubleClick += delegate { EditSelected(); };
            var contents = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty };
            contents.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            contents.RowStyles.Add(new RowStyle(SizeType.AutoSize)); contents.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            emptyState.Dock = DockStyle.Fill; emptyState.Name = "vaultEmptyState";
            emptyState.AccessibleName = "Состояние списка хранилища";
            contents.SizeChanged += delegate { emptyState.MaximumSize = new Size(Math.Max(1, contents.ClientSize.Width - emptyState.Margin.Horizontal), 0); };
            contents.Controls.Add(emptyState, 0, 0); contents.Controls.Add(grid, 0, 1);
            root.Controls.Add(contents, 0, 1);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var close = UiTheme.Button("Закрыть", null, false, DialogResult.Cancel);
            var lockButton = UiTheme.Button("Заблокировать", null, false, DialogResult.Cancel);
            var copy = UiTheme.Button("Копировать секрет", null, false);
            var delete = UiTheme.Button("Удалить", null, false);
            var edit = UiTheme.Button("Изменить", null, false);
            var add = UiTheme.Button("Добавить", null, false);
            close.AccessibleDescription = "Закрывает хранилище. Для следующего открытия потребуется PIN-код.";
            lockButton.AccessibleDescription = "Закрывает хранилище. Для следующего открытия потребуется PIN-код.";
            copy.AccessibleDescription = "Копирует секрет выбранной записи. " + clipboard.CopyNotice;
            delete.AccessibleDescription = "Удаляет выбранную запись после подтверждения. Удаление нельзя отменить.";
            edit.AccessibleDescription = "Открывает выбранную запись. Изменения применяются только после сохранения.";
            add.AccessibleDescription = "Открывает новую запись. Запись добавляется только после сохранения.";
            add.Click += delegate { AddEntry(); };
            edit.Click += delegate { EditSelected(); };
            delete.Click += delegate { DeleteSelected(); };
            copy.Click += delegate { CopySelected(); };
            buttons.Controls.Add(close);
            buttons.Controls.Add(lockButton);
            buttons.Controls.Add(copy);
            buttons.Controls.Add(delete);
            buttons.Controls.Add(edit);
            buttons.Controls.Add(add);
            root.Controls.Add(buttons, 0, 2);
            CancelButton = close;
            UiTheme.ConfigureKeyboardOrder(this);
            Reload();
        }

        private void Reload()
        {
            grid.Columns.Clear();
            grid.Rows.Clear();
            grid.Columns.Add("name", "Название");
            grid.Columns.Add("type", "Тип");
            grid.Columns.Add("login", "Логин");
            grid.Columns.Add("host", "Сайт / сервер");
            grid.Columns.Add("tags", "Метки");
            grid.Columns.Add("updated", "Изменено");

            var q = search.Text.Trim().ToLowerInvariant();
            foreach (var entry in session.Data.entries.OrderByDescending(e => e.updated_at))
            {
                if (!MatchesSearch(entry, q) || !MatchesType(entry)) continue;
                var row = grid.Rows.Add(entry.name, DisplayType(entry.type), entry.login, entry.url_or_host, entry.tags, entry.updated_at);
                grid.Rows[row].Tag = entry;
            }
            emptyState.Text = grid.Rows.Count != 0 ? "" : session.Data.entries.Count == 0
                ? "Записей пока нет. Добавить запись можно кнопкой «Добавить»."
                : "Совпадений нет. Измените поиск или выберите «Все» в фильтре.";
            emptyState.AccessibleDescription = emptyState.Text; emptyState.Visible = grid.Rows.Count == 0;
            grid.AccessibleDescription = GridDescription + (grid.Rows.Count == 0 ? " " + emptyState.Text : "");
        }

        private bool MatchesType(VaultEntry entry)
        {
            if (typeFilter.SelectedIndex <= 0) return true;
            return DisplayType(entry.type) == typeFilter.SelectedItem.ToString();
        }

        private static bool MatchesSearch(VaultEntry entry, string q)
        {
            if (String.IsNullOrEmpty(q)) return true;
            var hay = String.Join(" ", new[] { entry.name, entry.login, entry.url_or_host, entry.tags, entry.notes }).ToLowerInvariant();
            return hay.IndexOf(q, StringComparison.Ordinal) >= 0;
        }

        private VaultEntry SelectedEntry()
        {
            if (grid.SelectedRows.Count == 0) return null;
            return grid.SelectedRows[0].Tag as VaultEntry;
        }

        private void AddEntry()
        {
            var entry = VaultEntry.New();
            using (var form = new EntryForm(entry))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                session.Data.entries.Add(form.Entry);
                VaultService.Save(session);
                Reload();
            }
        }

        private void EditSelected()
        {
            var selected = SelectedEntry();
            if (selected == null) return;
            using (var form = new EntryForm(selected.Clone()))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                var index = session.Data.entries.FindIndex(e => e.id == selected.id);
                if (index >= 0) session.Data.entries[index] = form.Entry;
                VaultService.Save(session);
                Reload();
            }
        }

        private void DeleteSelected()
        {
            var selected = SelectedEntry();
            if (selected == null) return;
            var answer = MessageBox.Show("Удалить запись «" + selected.name + "»?\nЭто действие нельзя отменить.", "Удаление записи", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (answer != DialogResult.OK) return;
            session.Data.entries.RemoveAll(e => e.id == selected.id);
            VaultService.Save(session);
            Reload();
        }

        private void CopySelected()
        {
            var selected = SelectedEntry();
            if (selected == null) return;
            clipboard.CopySecret(selected.secret);
            MessageBox.Show("Секрет скопирован. " + clipboard.CopyNotice, "Хранилище секретов");
        }

        internal static string DisplayType(string type)
        {
            switch ((type ?? "").ToLowerInvariant())
            {
                case "api_key": return "API-ключ";
                case "password": return "Пароль";
                case "token": return "Токен";
                case "ssh": return "SSH";
                case "note": return "Заметка";
                case "custom": return "Другое";
                default: return "Другое";
            }
        }

        internal static string InternalType(string display)
        {
            switch (display)
            {
                case "API-ключ": return "api_key";
                case "Пароль": return "password";
                case "Токен": return "token";
                case "SSH": return "ssh";
                case "Заметка": return "note";
                default: return "custom";
            }
        }
    }

    internal sealed class EntryForm : ProGoForm
    {
        private readonly TextBox name = new TextBox();
        private readonly ComboBox type = new ComboBox();
        private readonly TextBox login = new TextBox();
        private readonly TextBox secret = new TextBox();
        private readonly TextBox host = new TextBox();
        private readonly TextBox tags = new TextBox();
        private readonly TextBox notes = new TextBox();
        public VaultEntry Entry { get; private set; }

        public EntryForm(VaultEntry entry)
        {
            Entry = entry;
            Text = String.IsNullOrEmpty(entry.name) ? "Новая запись" : "Редактирование записи";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(680, 570);
            MinimumSize = new Size(620, 540);

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = UiTheme.DialogPadding, ColumnCount = 2, RowCount = 9 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(table);

            type.DropDownStyle = ComboBoxStyle.DropDownList;
            type.Items.AddRange(new object[] { "API-ключ", "Пароль", "Токен", "SSH", "Заметка", "Другое" });
            secret.UseSystemPasswordChar = true;
            notes.Multiline = true;
            notes.Height = 80;

            Add(table, 0, "Название", name);
            Add(table, 1, "Тип", type);
            Add(table, 2, "Логин", login);
            Add(table, 3, "Секрет", secret);
            Add(table, 4, "Сайт / сервер", host);
            Add(table, 5, "Метки", tags);
            Add(table, 6, "Заметки", notes);

            var show = new CheckBox { Text = "Показать секрет", AutoSize = true };
            show.AccessibleDescription = "Показывает или скрывает содержимое секрета на экране. Не сохраняет запись.";
            secret.AccessibleDescription = "Содержимое секрета скрыто. Показать его можно флажком «Показать секрет».";
            show.CheckedChanged += delegate {
                secret.UseSystemPasswordChar = !show.Checked;
                secret.AccessibleDescription = show.Checked ? "Содержимое секрета отображается на экране." : "Содержимое секрета скрыто. Показать его можно флажком «Показать секрет».";
            };
            table.Controls.Add(show, 1, 7);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var save = UiTheme.Button("Сохранить", null, true, DialogResult.OK);
            var cancel = UiTheme.Button("Отмена", null, false, DialogResult.Cancel);
            save.AccessibleDescription = "Проверяет поля и сохраняет запись в хранилище.";
            cancel.AccessibleDescription = "Закрывает редактор без сохранения изменений.";
            save.Click += Save;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            table.Controls.Add(buttons, 0, 8);
            table.SetColumnSpan(buttons, 2);
            AcceptButton = save;
            CancelButton = cancel;
            LoadEntry(entry);
            UiTheme.ConfigureKeyboardOrder(this);
        }

        private static void Add(TableLayoutPanel table, int row, string label, Control control)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, row == 6 ? 92 : 42));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.AccessibleName = label;
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }

        private void LoadEntry(VaultEntry entry)
        {
            name.Text = entry.name ?? "";
            type.SelectedItem = VaultForm.DisplayType(entry.type);
            if (type.SelectedIndex < 0) type.SelectedItem = "Другое";
            login.Text = entry.login ?? "";
            secret.Text = entry.secret ?? "";
            host.Text = entry.url_or_host ?? "";
            tags.Text = entry.tags ?? "";
            notes.Text = entry.notes ?? "";
        }

        private void Save(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(name.Text))
            {
                MessageBox.Show("Укажите название записи.", "Проверьте заполненные поля.");
                DialogResult = DialogResult.None;
                return;
            }
            var internalType = VaultForm.InternalType(type.SelectedItem == null ? "Другое" : type.SelectedItem.ToString());
            if (internalType != "note" && String.IsNullOrEmpty(secret.Text))
            {
                MessageBox.Show("Укажите секрет.", "Проверьте заполненные поля.");
                DialogResult = DialogResult.None;
                return;
            }

            Entry.name = name.Text.Trim();
            Entry.type = internalType;
            Entry.login = login.Text.Trim();
            Entry.secret = secret.Text;
            Entry.url_or_host = host.Text.Trim();
            Entry.tags = tags.Text.Trim();
            Entry.notes = notes.Text;
            if (String.IsNullOrEmpty(Entry.created_at)) Entry.created_at = DateTimeOffset.UtcNow.ToString("o");
            Entry.updated_at = DateTimeOffset.UtcNow.ToString("o");
        }
    }

    internal sealed class PinForm : ProGoForm
    {
        private readonly TextBox pin = new TextBox();
        private readonly TextBox confirm = new TextBox();
        private readonly bool create;
        public string Pin { get { return pin.Text; } }

        public PinForm(bool createVault)
        {
            create = createVault;
            Text = create ? "Создание хранилища" : "Разблокировка";
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, create ? 260 : 220);
            MinimumSize = Size;

            var table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = UiTheme.DialogPadding, ColumnCount = 2, RowCount = create ? 4 : 3 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(table);
            pin.UseSystemPasswordChar = true;
            confirm.UseSystemPasswordChar = true;
            pin.AccessibleDescription = "Введите PIN-код из четырёх цифр. Ввод скрыт.";
            confirm.AccessibleDescription = "Повторите PIN-код из четырёх цифр. Ввод скрыт.";
            Add(table, 0, "PIN-код", pin);
            if (create) Add(table, 1, "Повторите PIN-код", confirm);
            var hint = new Label { Text = "PIN-код должен содержать ровно 4 цифры.", AutoSize = true };
            table.Controls.Add(hint, 0, create ? 2 : 1);
            table.SetColumnSpan(hint, 2);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var ok = UiTheme.Button(create ? "Создать" : "Открыть", null, true, DialogResult.OK);
            var cancel = UiTheme.Button("Отмена", null, false, DialogResult.Cancel);
            ok.AccessibleDescription = create ? "Проверяет PIN-код и его повтор. Создание хранилища выполняется после подтверждения." : "Передаёт PIN-код для открытия хранилища. Доступ зависит от проверки PIN-кода.";
            cancel.AccessibleDescription = "Закрывает окно без создания или открытия хранилища.";
            ok.Click += ValidatePin;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            table.Controls.Add(buttons, 0, create ? 3 : 2);
            table.SetColumnSpan(buttons, 2);
            AcceptButton = ok;
            CancelButton = cancel;
            UiTheme.ConfigureKeyboardOrder(this);
        }

        private static void Add(TableLayoutPanel table, int row, string label, Control control)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.AccessibleName = label;
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }

        private void ValidatePin(object sender, EventArgs e)
        {
            if (pin.Text.Length != 4 || !pin.Text.All(Char.IsDigit))
            {
                MessageBox.Show("PIN-код должен содержать ровно 4 цифры.", Text);
                DialogResult = DialogResult.None;
                return;
            }
            if (create && pin.Text != confirm.Text)
            {
                MessageBox.Show("PIN-коды не совпадают.", Text);
                DialogResult = DialogResult.None;
            }
        }

        public static bool OpenSession(out VaultSession session)
        {
            session = null;
            var create = !VaultService.Exists();
            using (var form = new PinForm(create))
            {
                if (form.ShowDialog() != DialogResult.OK) return false;
                try
                {
                    session = create ? VaultService.Create(form.Pin) : VaultService.Open(form.Pin);
                    return true;
                }
                catch (Exception ex)
                {
                    SafeLog.Error("Vault unlock/create failed.", ex);
                    MessageBox.Show("Не удалось открыть хранилище.", AppConstants.ProductName);
                    return false;
                }
            }
        }
    }

}
