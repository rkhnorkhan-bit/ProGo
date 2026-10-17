using System;
using System.Globalization;
using System.Security.Cryptography;

namespace ProGo
{
    internal static class VaultFakeDataFactory
    {
        // Fresh independent records. Neither PIN nor real records are inputs.
        internal static VaultData Create(DateTime localNow)
        {
            VaultDailyPolicy.RequireLocalTime(localNow);
            var data = VaultData.Empty(); var created = localNow.ToString("o", CultureInfo.InvariantCulture);
            data.entries.Add(Entry("Рабочая почта", "password", "office", "mail.office.local", "Контактный адрес", "work", created));
            data.entries.Add(Entry("Сервер разработки", "ssh", "deploy", "dev.office.local", "Рабочий доступ", "server", created));
            data.entries.Add(Entry("Заметки", "note", "", "", "Список задач на неделю", "notes", created));
            return data;
        }
        private static VaultEntry Entry(string name, string type, string login, string host, string notes, string tags, string created)
        {
            byte[] bytes = new byte[24]; string secret;
            try { using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes); secret = Convert.ToBase64String(bytes); }
            finally { Array.Clear(bytes, 0, bytes.Length); }
            return new VaultEntry { id = Guid.NewGuid().ToString("N"), name = name, type = type, login = login,
                secret = secret, url_or_host = host, notes = notes, tags = tags, created_at = created, updated_at = created };
        }
    }
}
