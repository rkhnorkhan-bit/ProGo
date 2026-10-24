using System;
using System.Collections.Generic;

namespace ProGo
{
    // Immutable ownership metadata; FakeData stays mutable so accepted edits retain identity.
    internal sealed class VaultDailyState
    {
        internal DateTime LocalDay { get; private set; }
        internal string FakePin { get; private set; }
        internal VaultData FakeData { get; private set; }
        internal bool HasFake { get { return FakeData != null; } }

        private VaultDailyState(DateTime day, string pin, VaultData data)
        { LocalDay = day; FakePin = pin; FakeData = data; }

        internal static VaultDailyState Empty(DateTime localNow)
        {
            VaultDailyPolicy.RequireLocalTime(localNow);
            return new VaultDailyState(DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified), null, null);
        }

        internal static VaultDailyState Bind(DateTime localNow, string pin, VaultData data)
        {
            VaultDailyPolicy.RequireLocalTime(localNow);
            if (!VaultDailyPolicy.IsValidPin(pin) || !VaultDailyPolicy.IsValidData(data))
                throw new ArgumentException("Invalid daily vault state.");
            return new VaultDailyState(DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified), pin, data);
        }
    }

    internal enum VaultDailyOutcome { Rejected, RealOpened, FakeOpened, FakePendingCommit }

    internal sealed class VaultDailyTransition
    {
        internal VaultDailyOutcome Outcome { get; private set; }
        // Adopt this expiry baseline even if opening fails. It never contains a new binding.
        internal VaultDailyState StateAfterExpiry { get; private set; }
        // Only the later atomic store may authorize adoption/publication of this candidate.
        internal VaultDailyState PendingFakeState { get; private set; }
        internal VaultData CandidateData { get; private set; }
        internal string ErrorMessage { get; private set; }
        internal bool RequiresFakeCommit { get { return Outcome == VaultDailyOutcome.FakePendingCommit; } }
        internal bool CanOpen { get { return Outcome == VaultDailyOutcome.RealOpened || Outcome == VaultDailyOutcome.FakeOpened; } }

        private VaultDailyTransition(VaultDailyOutcome outcome, VaultDailyState baseline,
            VaultDailyState pending, VaultData data, string error)
        {
            Outcome = outcome; StateAfterExpiry = baseline; PendingFakeState = pending;
            CandidateData = data; ErrorMessage = error;
        }

        internal static VaultDailyTransition Reject(VaultDailyState baseline, string error)
        {
            if (baseline == null || String.IsNullOrEmpty(error))
                throw new ArgumentException("Invalid daily vault transition.");
            return new VaultDailyTransition(VaultDailyOutcome.Rejected, baseline, null, null, error);
        }
        internal static VaultDailyTransition Open(VaultDailyState baseline, VaultData data, bool real)
        {
            if (baseline == null || !VaultDailyPolicy.IsValidData(data) ||
                (!real && !Object.ReferenceEquals(baseline.FakeData, data)))
                throw new ArgumentException("Invalid daily vault transition.");
            return new VaultDailyTransition(real ? VaultDailyOutcome.RealOpened : VaultDailyOutcome.FakeOpened,
                baseline, null, data, null);
        }
        internal static VaultDailyTransition Pending(VaultDailyState baseline, VaultDailyState pending)
        {
            if (baseline == null || baseline.HasFake || pending == null || !pending.HasFake ||
                baseline.LocalDay != pending.LocalDay || !VaultDailyPolicy.IsValidData(pending.FakeData))
                throw new ArgumentException("Invalid daily vault transition.");
            return new VaultDailyTransition(VaultDailyOutcome.FakePendingCommit, baseline, pending, pending.FakeData, null);
        }
    }

    internal static class VaultDailyPolicy
    {
        private const string InvalidPin = "PIN-код должен содержать ровно 4 цифры.";
        private const string WrongPin = "Неверный пин-код";
        private const string InvalidData = "Формат или данные хранилища повреждены.";
        private const string FakeFailure = "Не удалось подготовить хранилище.";

        // knownRealData is a caller-held real dataset, if any; it is never retained here.
        internal static VaultDailyTransition Evaluate(VaultDailyState state, DateTime localNow, string pin,
            VaultReadResult realRead, Func<VaultData> fakeDataFactory, VaultData knownRealData = null)
        {
            if (state == null) throw new ArgumentNullException("state");
            RequireLocalTime(localNow);
            var baseline = state.LocalDay == localNow.Date ? state : VaultDailyState.Empty(localNow);
            if (!IsValidPin(pin)) return VaultDailyTransition.Reject(baseline, InvalidPin);
            if (realRead == null) return VaultDailyTransition.Reject(baseline, InvalidData);
            if (realRead.Status == VaultReadStatus.Success) {
                if (!IsValidData(realRead.Data))
                    return VaultDailyTransition.Reject(baseline, InvalidData);
                return VaultDailyTransition.Open(baseline, realRead.Data, true);
            }
            if (realRead.Status != VaultReadStatus.AuthenticationMismatch)
                return VaultDailyTransition.Reject(baseline, ReadFailure(realRead.Status));
            if (baseline.HasFake) {
                if (!String.Equals(pin, baseline.FakePin, StringComparison.Ordinal))
                    return VaultDailyTransition.Reject(baseline, WrongPin);
                if (!IsValidData(baseline.FakeData) || SharesData(baseline.FakeData, knownRealData))
                    return VaultDailyTransition.Reject(baseline, InvalidData);
                return VaultDailyTransition.Open(baseline, baseline.FakeData, false);
            }
            if (fakeDataFactory == null) return VaultDailyTransition.Reject(baseline, FakeFailure);
            VaultData generated;
            try { generated = fakeDataFactory(); }
            catch (Exception) { return VaultDailyTransition.Reject(baseline, FakeFailure); }
            if (!IsValidData(generated) || SharesData(generated, state.FakeData) || SharesData(generated, knownRealData))
                return VaultDailyTransition.Reject(baseline, FakeFailure);
            return VaultDailyTransition.Pending(baseline, VaultDailyState.Bind(localNow, pin, generated));
        }

        internal static void RequireLocalTime(DateTime localNow)
        {
            if (localNow.Kind == DateTimeKind.Utc)
                throw new ArgumentException("Local calendar time is required.", "localNow");
        }

        internal static bool IsValidPin(string pin)
        {
            if (String.IsNullOrEmpty(pin) || pin.Length != 4) return false;
            for (int i = 0; i < pin.Length; i++) if (!Char.IsDigit(pin[i])) return false;
            return true;
        }

        internal static bool IsValidData(VaultData data)
        {
            if (data == null || data.version != 1 || data.entries == null) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in data.entries)
                if (entry == null || String.IsNullOrEmpty(entry.id) || !ids.Add(entry.id)) return false;
            return true;
        }

        private static bool SharesData(VaultData first, VaultData second)
        {
            if (first == null || second == null) return false;
            if (Object.ReferenceEquals(first, second) || Object.ReferenceEquals(first.entries, second.entries)) return true;
            if (first.entries == null || second.entries == null) return false;
            var entries = new HashSet<VaultEntry>(second.entries);
            foreach (var entry in first.entries) if (entries.Contains(entry)) return true;
            return false;
        }

        private static string ReadFailure(VaultReadStatus status)
        {
            switch (status) {
                case VaultReadStatus.FileUnavailable:
                    return "Не удалось прочитать хранилище. Проверьте доступность файла и права доступа.";
                case VaultReadStatus.UnsupportedFormat:
                    return "Формат или параметры хранилища не поддерживаются.";
                default: return InvalidData;
            }
        }
    }
}
