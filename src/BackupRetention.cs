using System;
using System.Collections.Generic;
using System.IO;

namespace ProGo
{
    // Standalone installed source, also compiled into the desktop application.
    public sealed class BackupRetentionCandidate
    {
        public string Path { get; internal set; }
        internal string Manifest;
    }

    public sealed class BackupRetentionPlan
    {
        public string Root { get; internal set; }
        public List<BackupRetentionCandidate> Candidates { get; private set; }
        public int Total { get; internal set; }
        public BackupRetentionPlan() { Candidates = new List<BackupRetentionCandidate>(); }
    }

    public sealed class BackupRetentionResult
    {
        public int Deleted { get; internal set; }
        public int Kept { get; internal set; }
        public int Failed { get; internal set; }
        public int Skipped { get; internal set; }
    }

    public static class BackupRetention
    {
        private const int MaxAutomaticBackups = 10;

        public static BackupRetentionPlan Plan(string root) { return Plan(root, null); }

        public static BackupRetentionPlan Plan(string root, string activeBackup)
        {
            var plan = new BackupRetentionPlan { Root = System.IO.Path.GetFullPath(root) };
            if (!Directory.Exists(plan.Root)) return plan;
            var dirs = Directory.GetDirectories(plan.Root);
            plan.Total = dirs.Length;
            if (IsLink(plan.Root)) return plan;
            // Generated names begin with a sortable timestamp. Use one ordering
            // in both runtimes; changing file timestamps cannot age a backup.
            Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
            Array.Reverse(dirs);
            int automatic = 0;
            bool baseline = false, preUpdate = false;
            foreach (var dir in dirs)
            {
                string manifest, kind;
                if (!TryReadAutomatic(dir, out manifest, out kind)) continue;
                bool protectedKind = (kind == "baseline" && !baseline) || (kind == "pre-update" && !preUpdate);
                if (kind == "baseline") baseline = true;
                if (kind == "pre-update") preUpdate = true;
                bool recent = automatic++ < MaxAutomaticBackups;
                if (!protectedKind && !recent && !String.Equals(dir, activeBackup, StringComparison.OrdinalIgnoreCase))
                    plan.Candidates.Add(new BackupRetentionCandidate { Path = dir, Manifest = manifest });
            }
            return plan;
        }

        public static BackupRetentionResult Apply(BackupRetentionPlan approved)
        {
            if (approved == null) throw new ArgumentNullException("approved");
            var current = Plan(approved.Root);
            var eligible = new Dictionary<string, BackupRetentionCandidate>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in current.Candidates) eligible[item.Path] = item;
            var result = new BackupRetentionResult();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in approved.Candidates)
            {
                BackupRetentionCandidate live;
                if (!seen.Add(item.Path)) continue;
                if (!eligible.TryGetValue(item.Path, out live) || item.Manifest != live.Manifest)
                { result.Skipped++; continue; }
                try
                {
                    string manifest, kind;
                    // Never expand a confirmed list or follow a junction. A changed
                    // manifest must be reviewed again, including manual relabelling.
                    if (!TryReadAutomatic(item.Path, out manifest, out kind) || manifest != item.Manifest || HasLinks(item.Path))
                    { result.Skipped++; continue; }
                    Directory.Delete(item.Path, true);
                    result.Deleted++;
                }
                catch (IOException) { result.Failed++; }
                catch (UnauthorizedAccessException) { result.Failed++; }
            }
            result.Kept = current.Total - result.Deleted;
            return result;
        }

        private static bool IsLink(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }

        private static bool HasLinks(string path)
        {
            if (IsLink(path)) return true;
            foreach (var file in Directory.GetFiles(path)) if (IsLink(file)) return true;
            foreach (var dir in Directory.GetDirectories(path)) if (HasLinks(dir)) return true;
            return false;
        }

        private static bool TryReadAutomatic(string dir, out string manifest, out string kind)
        {
            manifest = null; kind = null;
            try
            {
                if (IsLink(dir)) return false;
                var file = System.IO.Path.Combine(dir, "manifest.txt");
                if (!File.Exists(file) || IsLink(file)) return false;
                manifest = File.ReadAllText(file);
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in manifest.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0) return false;
                    string key = line.Substring(0, separator).Trim();
                    if (values.ContainsKey(key)) return false;
                    values[key] = line.Substring(separator + 1).Trim();
                }
                string product, creator, status;
                if (!values.TryGetValue("product", out product) || !String.Equals(product, "ProGo", StringComparison.OrdinalIgnoreCase)) return false;
                if (!values.TryGetValue("backup_kind", out kind)) return false;
                kind = kind.ToLowerInvariant();
                values.TryGetValue("created_by", out creator); values.TryGetValue("update_result", out status);
                if (kind == "manual" || String.Equals(creator, "manual", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals(status, "manual", StringComparison.OrdinalIgnoreCase)) return false;
                return kind == "baseline" || kind == "pre-update" || kind == "automatic";
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }
    }
}
