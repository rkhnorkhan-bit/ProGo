Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$SuiteWatch = [Diagnostics.Stopwatch]::StartNew()
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=UpdatePackageTests.ps1'
try {
    & (Join-Path $Root 'tests\UpdatePackageTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=UpdatePackageTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallPayloadTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallPayloadTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallPayloadTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerMaintenanceTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerMaintenanceTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerMaintenanceTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerStageTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerStageTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerStageTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerBackupTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerBackupTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerBackupTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerSelfCheckTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerSelfCheckTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerSelfCheckTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerRollbackTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerRollbackTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerRollbackTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerJournalTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerJournalTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerJournalTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerRestartGateTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerRestartGateTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerRestartGateTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerOutcomeTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerOutcomeTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerOutcomeTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerRollbackOutcomeTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerRollbackOutcomeTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerRollbackOutcomeTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerOutcomeReconcileTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerOutcomeReconcileTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerOutcomeReconcileTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerPendingInstalledProofTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerPendingInstalledProofTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerPendingInstalledProofTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerStartupEvidenceTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerStartupEvidenceTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerStartupEvidenceTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerMenuEvidenceTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerMenuEvidenceTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerMenuEvidenceTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerLegacyEvidenceTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerLegacyEvidenceTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerLegacyEvidenceTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerAuxiliaryRevalidationTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerAuxiliaryRevalidationTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerAuxiliaryRevalidationTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerPendingPublicationTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerPendingPublicationTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerPendingPublicationTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerStartupRollbackAdmissionTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerStartupRollbackAdmissionTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerStartupRollbackAdmissionTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerOwnedStartupRollbackTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerOwnedStartupRollbackTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerOwnedStartupRollbackTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerStartupResultTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerStartupResultTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerStartupResultTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerStartupMutationAdmissionTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerStartupMutationAdmissionTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerStartupMutationAdmissionTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerStartupFailureTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerStartupFailureTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerStartupFailureTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerMenuPublicationTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerMenuPublicationTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerMenuPublicationTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerMenuRollbackAdmissionTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerMenuRollbackAdmissionTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerMenuRollbackAdmissionTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerOwnedMenuRollbackTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerOwnedMenuRollbackTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerOwnedMenuRollbackTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerMenuMutationAdmissionTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerMenuMutationAdmissionTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerMenuMutationAdmissionTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerMenuResultTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerMenuResultTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerMenuResultTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerMenuFailureTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerMenuFailureTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerMenuFailureTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$CaseWatch = [Diagnostics.Stopwatch]::StartNew()
Write-Host 'INSTALLER_SUITE_START name=InstallerLegacyRollbackAdmissionTests.ps1'
try {
    & (Join-Path $Root 'tests\InstallerLegacyRollbackAdmissionTests.ps1')
    Write-Host ('INSTALLER_SUITE_PASS name=InstallerLegacyRollbackAdmissionTests.ps1; elapsedMs=' + $CaseWatch.ElapsedMilliseconds)
} finally { $CaseWatch.Stop() }
$SuiteWatch.Stop()
Write-Host ('Installer partition PASS: 30 suites; elapsedMs=' + $SuiteWatch.ElapsedMilliseconds)
