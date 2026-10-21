# Доказательства актуального остатка: повторная сверка d0f9a690

Это evidence для единого [реестра](PROJECT_STATE.md), не отдельный план.
Проверен source `d0f9a6902345eab288b25e8f902df1514db3f2ca` независимыми
read-only исполнителями. Приложение, repair, HomeVPN/службы/VPS не запускались.
Ниже FAIL означает подтверждённый source contract failure; конкретная latency,
ошибка на установленном ПК и native воспроизведение этих остатков NOT_RUN.

## F22-R03 / A04: repair — FAIL

`Repair-ProGo.ps1` разрешает payload только как root/release. Для готового
package-layout получается release/release; для installed-layout — InstallDir/release,
а PS helper source и destination совпадают. Явного внешнего trusted payload нет.
`Copy-IfExists` пропускает missing source; success проверяет только наличие EXE.
Прямые Copy-Item -Force заменяют EXE до VERSION/helpers, без maintenance gate,
staging/backup/rollback. Даже tampered EXE может дать OK при NoLaunch.

Build/Install выпускают41 runtime path:3 основных +17 PS +12 C# +9 HomeVPN.
Repair покрывает29: отсутствуют Install-ProGo.ps1, InstalledUpdateTransport.cs,
UpdateInstallSession.cs и9 scripts/home-vpn файлов. Два C# helpers прямо нужны
Update-ProGo.Core.Initialize-UpdateTransport/Initialize-UpdateWindow; HomeVpnService.Admin
копирует7 server resources. Обычный Program.RunSelfCheck не проверяет этот состав.
Добавить один его вызов недостаточно. README/license в счёт runtime не включены.

Следующие блоки: общий runtime inventory и чистый payload validation; затем
repair transaction с maintenance/staging/snapshot/verified rollback; затем native
matrix. Reuse существующего updater ownership/hash/staging/rollback, не новый
независимый updater. Отдельный setup fallback по F23 остаётся TODO.

Проверка: каждый обязательный missing/tampered source — отказ до записи;
repo/package/installed+Unicode paths; offline local payload; reparse source/dest;
активное приложение/maintenance contender/pending proxy cleanup; locked VERSION
после EXE/дисковый отказ/self-check hang; interruption/retry; private/settings/vault
hashes сохраняются. Fixtures изолируют фактический Windows known-folder путь,
один temporary working directory этого не обеспечивает.

## F31-R08: logs/report/export — FAIL по UI I/O

- Tray/Help → Context.ShowDiagnosticPreview → DiagnosticPreview.Show →
  DiagnosticReport.Build/AddSource: UI читает9 источников до появления preview.
  Лимит64КиБ ограничивает объём, но не длительность файлового вызова.
- DiagnosticPreviewForm.SaveReport пишет выбранный файл на UI после SaveFileDialog.
- Context.OpenLogFile/OpenProGoFolder/OpenBackups создают/читают каталоги/файлы и
  запускают viewer на UI. Process.Start не ждёт его завершения; latency самого
  запуска не измерена.
- UI callbacks/таймеры → SafeLog.Write → BoundedLog.TryWrite: hash mutex name,
  disk write/rotation и два1МиБ tail. WaitOne(0) устраняет mutex wait, не disk stall.
- Installed updater Copy-LogToClipboard → Show-ProGoDiagnostics выполняет cold
  Add-Type/read VERSION/тот же preview Build в GUI Click.

Уже выполнены allowlist/redaction, bounded sources/rotation, snapshot parity
copy/save; DPAPI/private keys в report pipeline нет. Modal MessageBox сам по себе
не дефект и остаётся на owner. Исторический F25 PASS относится к privacy/limits,
не подтверждает responsiveness R08 на текущем source.

Следующий блок: tracked worker Build и save с прежним shared installed API;
затем отдельно bounded background logger и tracked viewer/folder open.
Проверка: held read/write heartbeat+cancel+single-flight+Close/Dispose/stopped pump;
huge/locked/legacy/archives и sanitization; изменённый после preview журнал не
меняет сохранённый snapshot; chooser Cancel не пишет; denied destination даёт
fixed error/retry; late completed write не переименовывается abort. Сохранить
keyboard/minimum/150% и installed fresh-process/source parity. Logger проверяется
при held rotation/overflow без UI flush wait; open — без late launch после cancel.

## F31-R09: private HomeVPN I/O — FAIL

Program.Main → HomeVpnService constructor → HomeVpnPrivateFiles.Load читает файл
и ProtectedData.Unprotect до Application.Run. ShareOrigin getter повторяет disk/DPAPI
на UI из HomeProfileShare.CreateConfigureForm и HomeVpnWizardForm.ShowQr.
UI acceptance callbacks saveOrigin/SetShareOrigin и HomeVpnShareRecovery.ConfirmAsync
выполняют DPAPI/ACL/write/Replace/delete. Wizard Advance → UseToken/SetHomeAddress
сохраняет private данные на UI; SaveProfile → Access.WriteProfile также пишет на UI.

Wizard.StartChannel → HomeVpnService.StartAsync до первого await вызывает Stop,
SecureDirectory/ACL и3 File.WriteAllText session secret/config. AdminAsync до первого
незавершённого await проверяет key, читает recovery, создаёт work/ACL и копирует helpers.
Read*Output и TextChanged → UpdateSetupRecoveryState/PendingOwner читают storage/DPAPI.
`async` сам по себе не переносит эти начальные фазы в worker.

Уже workers: ProxyService.StartTunnelAsync, HomeVpnPreparationProcess.CopyAsync/
CommandAsync native waits, CompleteAdminWorkAsync cleanup, portable import/export.
Request-ID/recovery lease и запрет автоматического повтора внешнего эффекта сохраняются.

После R10: immutable access/owner/home/epoch snapshot, session preparation и private
load/cache в worker; затем private persistence/admin local phases. Проверка:
held DPAPI/save/session heartbeat, cancel/access change/Dispose запрещают late spawn
и stale publication; source DPAPI bytes/foreign session/desktop proxy сохраняются.
Ни passphrase, ни plaintext session contents не выводятся в отчёты.

## F31-R10: phone Stop/cleanup — FAIL

Tray StopPhone/StopAll, wizard Stop и Context.CompleteShutdown вызывают синхронный
HomeVpnService.Stop → ProxyService.Dispose (gate, WaitForExit1000) → recursive session
Delete. Gate wait отдельно не ограничен. Stop сохраняет ownership при incomplete
process/session cleanup, но возвращает void. Wizard выводит stopped по Relay.IsRunning,
а CompleteShutdown всё равно вызывает ExitThread: неполная очистка не участвует
в подтверждении выхода. Это не утверждение об утечке на владельческом ПК.

Следующий связанный блок: owned single-flight StopAsync с честным результатом;
включение wizard/tray/StopAll/graceful shutdown; Cancel/Start settlement без late spawn.
Native fixture: настоящий child + locked ssh_config, incomplete→видимая ошибка→unlock→
explicit retry; heartbeat; чужая session, desktop proxy и credentials неизменны.
Ikev2RelayService.Stop закрывает sockets, но task полного receive/flow settlement
не предоставляет. Это гипотеза для проверки, не доказанная утечка.

R07 worker ACK gate не заменяет R10: финальный CompleteShutdown ещё содержит
указанный phone Stop. Общие UI/installed/F31 gates поэтому не считаются PASS.
