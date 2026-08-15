using BepInEx;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SephiriaQoL;

internal sealed class RunSummaryRecord
{
    internal DateTime TimestampUtc;
    internal float PlayedSeconds;
    internal List<UtilityOverlay.DamageEntry> Entries = new List<UtilityOverlay.DamageEntry>();
}

internal sealed class RunSummaryHistoryStore
{
    private const string Header = "sephiria-qol-run-history-v1";
    private readonly string _path = Path.Combine(Paths.ConfigPath, "dev.tempeste.sephiria.qol.run-history");
    private readonly int _limit;

    internal RunSummaryHistoryStore(int limit)
    {
        _limit = Math.Max(1, limit);
    }

    internal List<RunSummaryRecord> Load()
    {
        var records = new List<RunSummaryRecord>();
        if (!File.Exists(_path))
            return records;

        try
        {
            string[] lines = File.ReadAllLines(_path);
            if (lines.Length == 0 || lines[0] != Header)
                return records;

            RunSummaryRecord current = null;
            UtilityOverlay.DamageEntry player = null;
            foreach (string line in lines.Skip(1))
            {
                string[] parts = line.Split('|');
                if (parts.Length == 0)
                    continue;

                switch (parts[0])
                {
                    case "r" when parts.Length >= 3:
                        current = new RunSummaryRecord
                        {
                            TimestampUtc = new DateTime(ParseLong(parts[1]), DateTimeKind.Utc),
                            PlayedSeconds = ParseFloat(parts[2])
                        };
                        player = null;
                        break;
                    case "p" when current != null && parts.Length >= 10:
                        player = new UtilityOverlay.DamageEntry
                        {
                            Name = Decode(parts[1]),
                            Damage = ParseFloat(parts[2]),
                            AreaDamage = ParseFloat(parts[3]),
                            DamageTaken = ParseFloat(parts[4]),
                            Hp = ParseFloat(parts[5]),
                            MaxHp = ParseFloat(parts[6]),
                            IsDead = parts[7] == "1",
                            Color = new Color(ParseFloat(parts[8]), ParseFloat(parts[9]),
                                parts.Length > 10 ? ParseFloat(parts[10]) : 1f, 1f)
                        };
                        current.Entries.Add(player);
                        break;
                    case "s" when player != null && parts.Length >= 4:
                        player.Sources.Add(new UtilityOverlay.DamageSourceEntry
                        {
                            Name = Decode(parts[1]),
                            Element = (EDamageElementalType)ParseInt(parts[2]),
                            Damage = ParseFloat(parts[3])
                        });
                        break;
                    case "e" when current != null:
                        if (current.Entries.Count > 0)
                            records.Add(current);
                        current = null;
                        player = null;
                        break;
                }
            }
        }
        catch
        {
            return new List<RunSummaryRecord>();
        }

        return records.TakeLast(_limit).ToList();
    }

    internal void Save(IReadOnlyList<RunSummaryRecord> records)
    {
        var lines = new List<string> { Header };
        foreach (RunSummaryRecord record in records.TakeLast(_limit))
        {
            lines.Add(FormattableString.Invariant($"r|{record.TimestampUtc.Ticks}|{record.PlayedSeconds:R}"));
            foreach (UtilityOverlay.DamageEntry entry in record.Entries)
            {
                lines.Add(string.Format(CultureInfo.InvariantCulture,
                    "p|{0}|{1:R}|{2:R}|{3:R}|{4:R}|{5:R}|{6}|{7:R}|{8:R}|{9:R}",
                    Encode(entry.Name), entry.Damage, entry.AreaDamage, entry.DamageTaken,
                    entry.Hp, entry.MaxHp, entry.IsDead ? 1 : 0,
                    entry.Color.r, entry.Color.g, entry.Color.b));
                foreach (UtilityOverlay.DamageSourceEntry source in entry.Sources.Take(12))
                {
                    lines.Add(FormattableString.Invariant(
                        $"s|{Encode(source.Name)}|{(int)source.Element}|{source.Damage:R}"));
                }
            }
            lines.Add("e");
        }

        string temporaryPath = _path + ".tmp";
        File.WriteAllLines(temporaryPath, lines);
        File.Copy(temporaryPath, _path, true);
        File.Delete(temporaryPath);
    }

    private static string Encode(string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));

    private static string Decode(string value) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(value));

    private static float ParseFloat(string value) =>
        float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);

    private static int ParseInt(string value) =>
        int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);

    private static long ParseLong(string value) =>
        long.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
}
